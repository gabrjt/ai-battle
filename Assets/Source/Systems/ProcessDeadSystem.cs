using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ProcessDeadSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Destroy))]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<DeadDuration>
        {
            public NativeQueue<Entity>.Concurrent DestroyQueue;
            [ReadOnly] public float DeltaTime;

            public void Execute(Entity entity, int index, ref DeadDuration deadDuration)
            {
                deadDuration.Value -= DeltaTime;

                if (deadDuration.Value > 0) return;

                DestroyQueue.Enqueue(entity);
            }
        }

        private struct DestroyJob : IJob
        {
            public NativeQueue<Entity> DestroyQueue;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (DestroyQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.AddComponent(entity, new Destroy());
                    CommandBuffer.AddComponent(entity, new Disabled());
                }
            }
        }

        private NativeQueue<Entity> m_DestroyQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_DestroyQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ProcessJob
            {
                DestroyQueue = m_DestroyQueue.ToConcurrent(),
                DeltaTime = Time.deltaTime
            }.Schedule(this, inputDeps);

            inputDeps = new DestroyJob
            {
                DestroyQueue = m_DestroyQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer(),
            }.Schedule(inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_DestroyQueue.IsCreated)
            {
                m_DestroyQueue.Dispose();
            }
        }
    }
}