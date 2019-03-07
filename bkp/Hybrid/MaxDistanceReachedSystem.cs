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
    public class MaxDistanceReachedSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Destroy))]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<Translation, MaxSqrDistance>
        {
            public NativeQueue<MaxDistanceReached>.Concurrent MaxDistanceReachedQueue;

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref MaxSqrDistance maxSqrDistance)
            {
                if (math.distancesq(translation.Value, maxSqrDistance.Origin) < maxSqrDistance.Value) return;

                MaxDistanceReachedQueue.Enqueue(new MaxDistanceReached
                {
                    This = entity
                });
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<MaxDistanceReached> MaxDistanceReachedQueue;

            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public EntityArchetype Archetype;

            public void Execute()
            {
                while (MaxDistanceReachedQueue.TryDequeue(out var maxDistanceReachedComponent))
                {
                    var maxDistanceReached = CommandBuffer.CreateEntity(Archetype);
                    CommandBuffer.SetComponent(maxDistanceReached, maxDistanceReachedComponent);
                }
            }
        }

        private EntityArchetype m_Archetype;

        private NativeQueue<MaxDistanceReached> m_MaxDistanceReachedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<MaxDistanceReached>());

            m_MaxDistanceReachedQueue = new NativeQueue<MaxDistanceReached>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventSystem = World.GetExistingManager<EventCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                MaxDistanceReachedQueue = m_MaxDistanceReachedQueue.ToConcurrent(),
            }.Schedule(this, inputDeps);

            inputDeps = new ApplyJob
            {
                MaxDistanceReachedQueue = m_MaxDistanceReachedQueue,
                CommandBuffer = eventSystem.CreateCommandBuffer(),
                Archetype = m_Archetype
            }.Schedule(inputDeps);

            eventSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            if (m_MaxDistanceReachedQueue.IsCreated)
            {
                m_MaxDistanceReachedQueue.Dispose();
            }
        }
    }
}