using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateBefore(typeof(MoveSystem))]
    public class ProcessWalkSpeedModifierSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(Walking))]
        private struct ProcessJob : IJobProcessComponentData<WalkSpeedModifier, Motion>
        {
            public void Execute([ReadOnly] ref WalkSpeedModifier walkSpeedModifier, ref Motion motion)
            {
                motion.Value *= walkSpeedModifier.Value;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new ProcessJob().Schedule(this, inputDeps);
        }
    }
}