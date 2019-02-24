using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class IdleSystem : JobComponentSystem
    {
        [RequireSubtractiveComponent(typeof(SearchingForDestination), typeof(Destination), typeof(Target))]
        private struct Job : IJobProcessComponentDataWithEntity<Idle>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public EntityArchetype Archetype;

            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Idle idle)
            {
                if (idle.StartTime + idle.IdleTime > Time) return;

                var idleTimeExpired = EntityCommandBuffer.CreateEntity(index, Archetype);
                EntityCommandBuffer.SetComponent(index, idleTimeExpired, new IdleTimeExpired { This = entity });
            }
        }

        private EntityArchetype m_Archetype;

        [Inject]
        private EndFrameBarrier m_EndFrameBarrier;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<IdleTimeExpired>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().ToConcurrent(),
                Archetype = m_Archetype,
                Time = Time.time
            }.Schedule(this, inputDeps);
        }
    }
}