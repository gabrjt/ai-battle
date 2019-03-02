using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    public class UpdateTargetDestinationSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentDataWithEntity<Target, Position, AttackDistance, Destination>
        {
            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            public void Execute(Entity entity, int index,
                [ReadOnly] ref Target target,
                [ReadOnly] ref Position position,
                [ReadOnly] ref AttackDistance attackDistance,
                ref Destination destination)
            {
                if (PositionFromEntity.Exists(target.Value))
                {
                    var targetDestination = PositionFromEntity[target.Value].Value;
                    var distance = math.distance(position.Value, targetDestination);

                    if (distance < attackDistance.Min || distance > attackDistance.Max)
                    {
                        var direction = math.normalizesafe(targetDestination - position.Value);
                        destination.LastValue = destination.Value;
                        destination.Value = targetDestination - direction * attackDistance.Min;
                    }
                }
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