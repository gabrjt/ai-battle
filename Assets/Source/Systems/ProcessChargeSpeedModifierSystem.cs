using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(ProcessMotionSystem))]
    public class ProcessChargeSpeedModifierSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(Charging))]
        private struct ProcessJob : IJobProcessComponentData<ChargeSpeedModifier, Motion>
        {
            public void Execute([ReadOnly] ref ChargeSpeedModifier chargeSpeedModifier, ref Motion motion)
            {
                motion.Value *= chargeSpeedModifier.Value;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new ProcessJob().Schedule(this, inputDeps);
        }
    }
}