using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class IdleSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<Idle>
        {
            public NativeQueue<IdleTimeExpired>.Concurrent IdleTimeExpiredQueue;

            [ReadOnly]
            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Idle idle)
            {
                if (idle.StartTime + idle.Duration > Time) return;

                IdleTimeExpiredQueue.Enqueue(new IdleTimeExpired { This = entity });
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<IdleTimeExpired> IdleTimeExpiredQueue;

            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public EntityArchetype Archetype;

            public void Execute()
            {
                while (IdleTimeExpiredQueue.TryDequeue(out var idleTimeExpiredComponent))
                {
                    var idleTimeExpired = CommandBuffer.CreateEntity(Archetype);
                    CommandBuffer.SetComponent(idleTimeExpired, idleTimeExpiredComponent);
                }
            }
        }

        private EntityArchetype m_Archetype;

        private NativeQueue<IdleTimeExpired> m_IdleTimeExpiredQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<IdleTimeExpired>());

            m_IdleTimeExpiredQueue = new NativeQueue<IdleTimeExpired>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventBarrier = World.GetExistingManager<EventBarrier>();

            inputDeps = new ConsolidateJob
            {
                IdleTimeExpiredQueue = m_IdleTimeExpiredQueue.ToConcurrent(),
                Time = Time.time
            }.Schedule(this, inputDeps);

            inputDeps = new ApplyJob
            {
                IdleTimeExpiredQueue = m_IdleTimeExpiredQueue,
                CommandBuffer = eventBarrier.CreateCommandBuffer(),
                Archetype = m_Archetype
            }.Schedule(inputDeps);

            eventBarrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            if (m_IdleTimeExpiredQueue.IsCreated)
            {
                m_IdleTimeExpiredQueue.Dispose();
            }
        }
    }
}