using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class AttackSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        public struct ConsolidateJob : IJobProcessComponentDataWithEntity<Attacking>
        {
            public NativeQueue<Entity>.Concurrent EntityQueue;

            [ReadOnly]
            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Attacking attack)
            {
                if (attack.StartTime + attack.Duration > Time) return;

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
                    CommandBuffer.RemoveComponent<Attacking>(entity);
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
            m_EntityQueue.Clear();

            var barrier = World.GetExistingManager<RemoveBarrier>();

            inputDeps = new ConsolidateJob
            {
                EntityQueue = m_EntityQueue.ToConcurrent(),
                Time = Time.time
            }.Schedule(this, inputDeps);

            inputDeps = new ApplyJob
            {
                EntityQueue = m_EntityQueue,
                CommandBuffer = barrier.CreateCommandBuffer(),
            }.Schedule(inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

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