using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(DisengageSystem))]
    public class ReachedDestinationSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Target))]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Translation, Destination>
        {
            public NativeQueue<Entity>.Concurrent ProcessedQueue;

            public void Execute(Entity entity, int index, [ReadOnly] ref Translation translation, [ReadOnly] ref Destination destination)
            {
                if (math.distancesq(translation.Value, destination.Value) > 0.01f) return;

                ProcessedQueue.Enqueue(entity);
            }
        }

        private NativeQueue<Entity> m_ProcessedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_ProcessedQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new ProcessJob
            {
                ProcessedQueue = m_ProcessedQueue.ToConcurrent()
            }.Schedule(this, inputDeps);

            inputDeps.Complete();

            while (m_ProcessedQueue.TryDequeue(out var entity))
            {
                EntityManager.RemoveComponent<Destination>(entity);
            }

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_ProcessedQueue.IsCreated)
            {
                m_ProcessedQueue.Dispose();
            }
        }
    }
}