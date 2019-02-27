using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Systems
{
    public class SetToOwnerRotationSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(Rotation), typeof(View))]
        private struct Job : IJobProcessComponentDataWithEntity<Owner>
        {
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Rotation> RotationFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Owner owner)
            {
                RotationFromEntity[entity] = RotationFromEntity[owner.Value];
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                RotationFromEntity = GetComponentDataFromEntity<Rotation>()
            }.Schedule(this, inputDeps);
        }
    }
}