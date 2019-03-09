using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ProcessIdleSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<IdleDuration>
        {
            public NativeQueue<Entity>.Concurrent RemoveQueue;
            [ReadOnly] public float DeltaTime;

            public void Execute(Entity entity, int index, ref IdleDuration idleDuration)
            {
                idleDuration.Value -= DeltaTime;

                if (idleDuration.Value > 0) return;

                RemoveQueue.Enqueue(entity);
            }
        }

        private struct RemoveJob : IJob
        {
            public NativeQueue<Entity> RemoveQueue;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (RemoveQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<Idle>(entity);
                    CommandBuffer.RemoveComponent<IdleDuration>(entity);
                }
            }
        }

        private NativeQueue<Entity> m_RemoveQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_RemoveQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ProcessJob
            {
                RemoveQueue = m_RemoveQueue.ToConcurrent(),
                DeltaTime = Time.deltaTime
            }.Schedule(this, inputDeps);

            inputDeps = new RemoveJob
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