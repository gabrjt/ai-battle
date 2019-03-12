using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class MoveSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(TargetInRange))]
        private struct Job : IJobProcessComponentData<Motion, Translation>
        {
            [ReadOnly] public float DeltaTime;

            public void Execute([ReadOnly] ref Motion motion, ref Translation translation)
            {
                translation.Value += motion.Value * DeltaTime;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                DeltaTime = UnityEngine.Time.deltaTime
            }.Schedule(this, inputDeps);
        }
    }
}