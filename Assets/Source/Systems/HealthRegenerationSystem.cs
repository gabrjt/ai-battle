using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class HealthRegenerationSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireSubtractiveComponent(typeof(Dead))]
        private struct Job : IJobProcessComponentData<Health, HealthRegeneration>
        {
            public float DeltaTime;

            public void Execute(ref Health health, [ReadOnly] ref HealthRegeneration healthRegeneration)
            {
                if (health.Value <= 0) return;

                health.Value += healthRegeneration.Value * DeltaTime;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                DeltaTime = Time.deltaTime
            }.Schedule(this, inputDeps);
        }
    }
}