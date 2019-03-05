using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class DeadSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<Dead>
        {
            public NativeQueue<Died>.Concurrent DiedQueue;
            [ReadOnly] public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Dead dead)
            {
                if (dead.Expired || dead.StartTime + dead.Duration > Time) return;

                dead.Expired = true;
                DiedQueue.Enqueue(new Died { This = entity });
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Died> DiedQueue;
            public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public EntityArchetype Archetype;

            public void Execute()
            {
                while (DiedQueue.TryDequeue(out var diedComponent))
                {
                    var died = CommandBuffer.CreateEntity(Archetype);
                    CommandBuffer.SetComponent(died, diedComponent);
                }
            }
        }

        private EntityArchetype m_Archetype;

        private NativeQueue<Died> m_DiedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<Died>());
            m_DiedQueue = new NativeQueue<Died>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventCommandBufferSystem = World.GetExistingManager<EventCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                DiedQueue = m_DiedQueue.ToConcurrent(),
                Time = Time.time
            }.Schedule(this, inputDeps);

            inputDeps = new ApplyJob
            {
                DiedQueue = m_DiedQueue,
                CommandBuffer = eventCommandBufferSystem.CreateCommandBuffer(),
                Archetype = m_Archetype,
            }.Schedule(inputDeps);

            eventCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();
            Dispose();
        }

        public void Dispose()
        {
            if (m_DiedQueue.IsCreated)
            {
                m_DiedQueue.Dispose();
            }
        }
    }
}