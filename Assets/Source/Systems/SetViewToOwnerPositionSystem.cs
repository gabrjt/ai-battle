using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Systems
{
    public class SetViewToOwnerPositionSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(Translation), typeof(View), typeof(Visible))]
        private struct Job : IJobProcessComponentDataWithEntity<Owner, Offset>
        {
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Translation> PositionFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Owner owner, [ReadOnly] ref Offset offset)
            {
                PositionFromEntity[entity] = new Translation { Value = PositionFromEntity[owner.Value].Value + offset.Value };
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                PositionFromEntity = GetComponentDataFromEntity<Translation>()
            }.Schedule(this, inputDeps);
        }
    }
}