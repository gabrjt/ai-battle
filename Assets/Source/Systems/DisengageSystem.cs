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
    public class DisengageSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Target, Translation, EngageSqrRadius>
        {
            public NativeQueue<Entity>.Concurrent DisengageQueue;
            [ReadOnly] public ComponentDataFromEntity<Dying> DyingFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Destroy> DestroyFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Target target, [ReadOnly] ref Translation translation, [ReadOnly] ref EngageSqrRadius engageSqrRadius)
            {
                if (math.distancesq(translation.Value, TranslationFromEntity[target.Value].Value) <= engageSqrRadius.Value &&
                    !DestroyFromEntity.Exists(target.Value) &&
                    !DyingFromEntity.Exists(target.Value)) return;

                DisengageQueue.Enqueue(entity);
            }
        }

        private struct DisengageJob : IJob
        {
            public NativeQueue<Entity> DisengageQueue;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (DisengageQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<Target>(entity);
                    CommandBuffer.RemoveComponent<Destination>(entity);
                }
            }
        }

        private NativeQueue<Entity> m_DisengageQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_DisengageQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ProcessJob
            {
                DisengageQueue = m_DisengageQueue.ToConcurrent(),
                DyingFromEntity = GetComponentDataFromEntity<Dying>(true),
                DestroyFromEntity = GetComponentDataFromEntity<Destroy>(true),
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(this, inputDeps);

            inputDeps = new DisengageJob
            {
                DisengageQueue = m_DisengageQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_DisengageQueue.IsCreated)
            {
                m_DisengageQueue.Dispose();
            }
        }
    }
}