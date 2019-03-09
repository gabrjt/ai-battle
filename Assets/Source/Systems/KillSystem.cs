using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(ClampHealthSystem))]
    public class KillSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Health>
        {
            public NativeQueue<Entity>.Concurrent DeadQueue;

            public void Execute(Entity entity, int index, [ReadOnly] ref Health health)
            {
                if (health.Value > 0) return;

                DeadQueue.Enqueue(entity);
            }
        }

        private struct AddDestroyJob : IJob
        {
            public NativeQueue<Entity> DeadQueue;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (DeadQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.AddComponent(entity, new Destroy());
                    CommandBuffer.AddComponent(entity, new Disabled());
                }
            }
        }

        private NativeQueue<Entity> m_DeadQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_DeadQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ProcessJob
            {
                DeadQueue = m_DeadQueue.ToConcurrent()
            }.Schedule(this, inputDeps);

            inputDeps = new AddDestroyJob
            {
                DeadQueue = m_DeadQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_DeadQueue.IsCreated)
            {
                m_DeadQueue.Dispose();
            }
        }
    }
}