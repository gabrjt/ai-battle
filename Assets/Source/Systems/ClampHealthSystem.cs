using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ClampHealthSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentData<Health, MaxHealth>
        {
            public void Execute(ref Health health, [ReadOnly] ref MaxHealth maxHealth)
            {
                health.Value = math.clamp(health.Value, 0, maxHealth.Value);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job { }.Schedule(this, inputDeps);
        }
    }
}