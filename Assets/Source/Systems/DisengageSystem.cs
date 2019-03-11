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
        [ExcludeComponent(typeof(Dead))]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Target, Translation, EngageSqrRadius>
        {
            public NativeQueue<Entity>.Concurrent RemoveQueue;
            [ReadOnly] public ComponentDataFromEntity<Dead> DeadFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Target target, [ReadOnly] ref Translation translation, [ReadOnly] ref EngageSqrRadius engageSqrRadius)
            {
                if (!DeadFromEntity.Exists(target.Value) &&
                    TranslationFromEntity.Exists(target.Value) &&
                    math.distancesq(translation.Value, TranslationFromEntity[target.Value].Value) <= engageSqrRadius.Value) return;

                RemoveQueue.Enqueue(entity);
            }
        }

        private struct RemoveTargetJob : IJob
        {
            public NativeQueue<Entity> RemoveQueue;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (RemoveQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<Target>(entity);
                    CommandBuffer.RemoveComponent<Destination>(entity);
                }
            }
        }

        private ComponentGroup m_DeadGroup;
        private NativeQueue<Entity> m_RemoveQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_DeadGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Dead>(), ComponentType.ReadWrite<Target>() }
            });

            m_RemoveQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            EntityManager.RemoveComponent(m_DeadGroup, ComponentType.ReadWrite<Target>());

            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ProcessJob
            {
                RemoveQueue = m_RemoveQueue.ToConcurrent(),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(this);

            inputDeps = new RemoveTargetJob
            {
                RemoveQueue = m_RemoveQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_RemoveQueue.IsCreated)
            {
                m_RemoveQueue.Dispose();
            }
        }
    }
}