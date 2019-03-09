using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class MoveSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentData<Components.Motion, Translation>
        {
            [ReadOnly] public float DeltaTime;

            public void Execute([ReadOnly] ref Components.Motion motion, ref Translation translation)
            {
                translation.Value += motion.Value * DeltaTime;
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