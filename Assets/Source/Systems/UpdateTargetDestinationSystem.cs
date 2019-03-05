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
        private struct Job : IJobProcessComponentDataWithEntity<Target, Translation, AttackDistance, Destination>
        {
            [ReadOnly]
            public ComponentDataFromEntity<Translation> PositionFromEntity;

            public void Execute(Entity entity, int index,
                [ReadOnly] ref Target target,
                [ReadOnly] ref Translation translation,
                [ReadOnly] ref AttackDistance attackDistance,
                ref Destination destination)
            {
                if (PositionFromEntity.Exists(target.Value))
                {
                    var targetDestination = PositionFromEntity[target.Value].Value;
                    var distance = math.distance(translation.Value, targetDestination);

                    if (distance < attackDistance.Min || distance > attackDistance.Max)
                    {
                        var direction = math.normalizesafe(targetDestination - translation.Value);
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
                PositionFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(this, inputDeps);
        }
    }
}