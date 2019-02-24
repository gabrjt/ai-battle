using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateAfter(typeof(HealthRegenerationSystem))]
    [UpdateAfter(typeof(DamageSystem))]
    public class ClampHealthSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ClampMaximumHealthJob : IJobProcessComponentData<Health, MaximumHealth>
        {
            public void Execute(ref Health health, [ReadOnly] ref MaximumHealth maximumHealth)
            {
                if (health.Value > maximumHealth.Value)
                {
                    health.Value = maximumHealth.Value;
                }
            }
        }

        [BurstCompile]
        private struct ClampMinimumHealthJob : IJobProcessComponentData<Health>
        {
            public void Execute(ref Health health)
            {
                if (health.Value < 0)
                {
                    health.Value = 0;
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var clampMaximumHealthHandle = new ClampMaximumHealthJob { }.Schedule(this, inputDeps);
            var clampMinimumHealthHandle = new ClampMinimumHealthJob { }.Schedule(this, clampMaximumHealthHandle);

            return clampMinimumHealthHandle;
        }
    }
}