using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    public class DestinationReachedSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        [RequireSubtractiveComponent(typeof(Target))]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<Destination, Position>
        {
            public NativeQueue<DestinationReached>.Concurrent DestinationReachedQueue;

            public void Execute(Entity entity, int index, [ReadOnly] ref Destination destination, [ReadOnly] ref Position position)
            {
                if (math.distance(new float3(destination.Value.x, 0, destination.Value.z), new float3(position.Value.x, 0, position.Value.z)) > 0.01f) return;

                DestinationReachedQueue.Enqueue(new DestinationReached { This = entity });
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<DestinationReached> DestinationReachedQueue;

            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public EntityArchetype Archetype;

            public void Execute()
            {
                while (DestinationReachedQueue.TryDequeue(out var destinationReachedComponent))
                {
                    var destinationReached = CommandBuffer.CreateEntity(Archetype);
                    CommandBuffer.SetComponent(destinationReached, destinationReachedComponent);
                }
            }
        }

        private EntityArchetype m_Archetype;

        private NativeQueue<DestinationReached> m_DestinationReachedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Event>(), ComponentType.Create<DestinationReached>());

            m_DestinationReachedQueue = new NativeQueue<DestinationReached>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_DestinationReachedQueue.Clear();

            var eventBarrier = World.GetExistingManager<EventBarrier>();

            inputDeps = new ConsolidateJob
            {
                DestinationReachedQueue = m_DestinationReachedQueue.ToConcurrent()
            }.Schedule(this, inputDeps);

            inputDeps = new ApplyJob
            {
                DestinationReachedQueue = m_DestinationReachedQueue,
                CommandBuffer = eventBarrier.CreateCommandBuffer(),
                Archetype = m_Archetype
            }.Schedule(inputDeps);

            eventBarrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            if (m_DestinationReachedQueue.IsCreated)
            {
                m_DestinationReachedQueue.Dispose();
            }
        }
    }
}