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
    [UpdateInGroup(typeof(LogicGroup))]
    public class ReachedDestinationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Target))]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<Destination, Translation, Velocity>
        {
            public NativeQueue<DestinationReached>.Concurrent DestinationReachedQueue;

            public void Execute(Entity entity, int index, [ReadOnly] ref Destination destination, [ReadOnly] ref Translation translation, [ReadOnly] ref Velocity velocity)
            {
                if (math.distance(new float3(destination.Value.x, 0, destination.Value.z), new float3(translation.Value.x, 0, translation.Value.z)) > math.lengthsq(velocity.Value) * 0.01f) return;

                DestinationReachedQueue.Enqueue(new DestinationReached { This = entity });
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<DestinationReached> DestinationReachedQueue;
            public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public EntityArchetype Archetype;

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

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<DestinationReached>());
            m_DestinationReachedQueue = new NativeQueue<DestinationReached>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventCommandBufferSystem = World.GetExistingManager<EventCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                DestinationReachedQueue = m_DestinationReachedQueue.ToConcurrent()
            }.Schedule(this, inputDeps);

            inputDeps = new ApplyJob
            {
                DestinationReachedQueue = m_DestinationReachedQueue,
                CommandBuffer = eventCommandBufferSystem.CreateCommandBuffer(),
                Archetype = m_Archetype
            }.Schedule(inputDeps);

            eventCommandBufferSystem.AddJobHandleForProducer(inputDeps);

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