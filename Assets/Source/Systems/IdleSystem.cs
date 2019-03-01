using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    // TODO: use Burst.
    
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
                if (idle.StartTime + idle.Duration > Time) return;

                var idleTimeExpired = EntityCommandBuffer.CreateEntity(index, Archetype);
                EntityCommandBuffer.SetComponent(index, idleTimeExpired, new IdleTimeExpired { This = entity });
            }
        }

        private EntityArchetype m_Archetype;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<IdleTimeExpired>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var barrier = World.GetExistingManager<EventBarrier>();

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