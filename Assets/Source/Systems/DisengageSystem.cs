using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class DisengageSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Target>
        {
            public NativeQueue<Entity>.Concurrent RemoveTargetQueue;
            [ReadOnly] public ComponentDataFromEntity<Dying> DyingFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Destroy> DestroyFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Target target)
            {
                if (DyingFromEntity.Exists(target.Value) || DestroyFromEntity.Exists(target.Value)) return;

                RemoveTargetQueue.Equals(entity);
            }
        }

        private struct DisengageJob : IJob
        {
            public NativeQueue<Entity> RemoveTargetQueue;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (RemoveTargetQueue.TryDequeue(out var entity))
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
                RemoveTargetQueue = m_DisengageQueue.ToConcurrent(),
                DyingFromEntity = GetComponentDataFromEntity<Dying>(true),
                DestroyFromEntity = GetComponentDataFromEntity<Destroy>(true)
            }.Schedule(this, inputDeps);

            inputDeps = new DisengageJob
            {
                RemoveTargetQueue = m_DisengageQueue,
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