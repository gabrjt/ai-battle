﻿using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class AttackSystem : JobComponentSystem
    {
        public struct Job : IJobProcessComponentDataWithEntity<Attack>
        {
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public float Time;

            public void Execute(Entity entity, int index, [ReadOnly] ref Attack attack)
            {
                if (attack.StartTime + attack.Value > Time) return;

                EntityCommandBuffer.RemoveComponent<Attack>(index, entity);
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