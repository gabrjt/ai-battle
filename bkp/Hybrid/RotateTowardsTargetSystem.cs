using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    public class RotateTowardsTargetSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentData<Translation, Target, Rotation>
        {
            [ReadOnly]
            public ComponentDataFromEntity<Translation> PositionFromEntity;

            public void Execute([ReadOnly] ref Translation translation, [ReadOnly] ref Target target, ref Rotation rotation)
            {
                rotation.Value = quaternion.LookRotation(PositionFromEntity[target.Value].Value - translation.Value, math.up());
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                PositionFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(this, inputDeps);
        }
    }
}