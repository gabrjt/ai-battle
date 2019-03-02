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
        [RequireComponentTag(typeof(Position), typeof(View), typeof(Visible))]
        private struct Job : IJobProcessComponentDataWithEntity<Owner, Offset>
        {
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Owner owner, [ReadOnly] ref Offset offset)
            {
                PositionFromEntity[entity] = new Position { Value = PositionFromEntity[owner.Value].Value + offset.Value };
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                PositionFromEntity = GetComponentDataFromEntity<Position>()
            }.Schedule(this, inputDeps);
        }
    }
}