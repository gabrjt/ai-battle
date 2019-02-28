using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class DeadSystem : JobComponentSystem
    {
        private struct Job : IJobProcessComponentDataWithEntity<Dead>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public EntityArchetype Archetype;

            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Dead dead)
            {
                if (dead.StartTime + dead.Duration > Time) return;

                var died = EntityCommandBuffer.CreateEntity(index, Archetype);
                EntityCommandBuffer.SetComponent(index, died, new Died { This = entity });
            }
        }

        private EntityArchetype m_Archetype;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<Died>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new Job
            {
                EntityCommandBuffer = barrier.CreateCommandBuffer().ToConcurrent(),
                Archetype = m_Archetype,
                Time = Time.time
            }.Schedule(this, inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}