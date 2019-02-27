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
        struct Job : IJobProcessComponentData<Position, Target, Rotation>
        {
            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            public void Execute([ReadOnly] ref Position position, [ReadOnly] ref Target target, ref Rotation rotation)
            {
                if (!PositionFromEntity.Exists(target.Value)) return;

                rotation.Value = quaternion.LookRotation(PositionFromEntity[target.Value].Value - position.Value, math.up());
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                PositionFromEntity = GetComponentDataFromEntity<Position>(true)
            }.Schedule(this, inputDeps);
        }
    }
}