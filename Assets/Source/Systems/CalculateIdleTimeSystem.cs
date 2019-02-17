using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class CalculateIdleTimeSystem : JobComponentSystem
    {
        [RequireSubtractiveComponent(typeof(Target))]
        private struct Job : IJobProcessComponentDataWithEntity<Idle>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Idle idle)
            {
                if (idle.StartTime + idle.IdleTime >= Time) return;

                EntityCommandBuffer.RemoveComponent<Idle>(index, entity);
                EntityCommandBuffer.AddComponent(index, entity, new SearchingForDestination());
            }
        }

        [Inject]
        private EndFrameBarrier m_EndFrameBarrier;

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                EntityCommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().ToConcurrent(),
                Time = Time.time
            }.Schedule(this, inputDeps);
        }
    }
}