using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Systems
{
    public class SetViewToOwnerRotationSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(Rotation), typeof(View), typeof(Visible))]
        private struct Job : IJobProcessComponentDataWithEntity<Owner>
        {
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Rotation> RotationFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Owner owner)
            {
                if (!RotationFromEntity.Exists(owner.Value)) return;

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