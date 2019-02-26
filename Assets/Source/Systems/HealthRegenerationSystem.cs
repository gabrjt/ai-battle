using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    [UpdateBefore(typeof(ClampHealthSystem))]
    public class HealthRegenerationSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentData<Health, HealthRegeneration>
        {
            public float DeltaTime;

            public void Execute(ref Health health, [ReadOnly] ref HealthRegeneration healthRegeneration)
            {
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