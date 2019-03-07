using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class CooldownSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        public struct ConsolidadteJob : IJobProcessComponentDataWithEntity<Cooldown>
        {
            public NativeQueue<Entity>.Concurrent EntityQueue;

            [ReadOnly]
            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Cooldown cooldown)
            {
                if (cooldown.StartTime + cooldown.Duration > Time) return;

                EntityQueue.Enqueue(entity);
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Entity> EntityQueue;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (EntityQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<Cooldown>(entity);
                }
            }
        }

        private NativeQueue<Entity> m_EntityQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_EntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var removeSystem = World.GetExistingManager<RemoveCommandBufferSystem>();

            inputDeps = new ConsolidadteJob
            {
                EntityQueue = m_EntityQueue.ToConcurrent(),
                Time = Time.time
            }.Schedule(this, inputDeps);

            inputDeps = new ApplyJob
            {
                EntityQueue = m_EntityQueue,
                CommandBuffer = removeSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            removeSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            if (m_EntityQueue.IsCreated)
            {
                m_EntityQueue.Dispose();
            }
        }
    }
}