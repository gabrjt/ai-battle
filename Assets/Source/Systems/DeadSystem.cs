using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class DeadSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<Dead, Health>
        {
            public NativeQueue<Died>.Concurrent DiedQueue;

            [ReadOnly]
            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Dead dead, ref Health health)
            {
                health.Value = 0;

                if (dead.StartTime + dead.Duration > Time) return;

                DiedQueue.Enqueue(new Died { This = entity });
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Died> DiedQueue;

            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public EntityArchetype Archetype;

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

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<Died>());

            m_DiedQueue = new NativeQueue<Died>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_DiedQueue.Clear();

            var barrier = World.GetExistingManager<EventBarrier>();

            inputDeps = new ConsolidateJob
            {
                DiedQueue = m_DiedQueue.ToConcurrent(),
                Time = Time.time
            }.Schedule(this, inputDeps);

            inputDeps = new ApplyJob
            {
                DiedQueue = m_DiedQueue,
                CommandBuffer = barrier.CreateCommandBuffer(),
                Archetype = m_Archetype,
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
            if (m_DiedQueue.IsCreated)
            {
                m_DiedQueue.Dispose();
            }
        }
    }
}