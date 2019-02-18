using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class CooldownSystem : JobComponentSystem
    {
        public struct Job : IJobProcessComponentDataWithEntity<Cooldown>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Cooldown cooldown)
            {
                if (cooldown.StartTime + cooldown.Value > Time) return;

                EntityCommandBuffer.RemoveComponent<Cooldown>(index, entity);
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