using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class UpdateTargetDestinationSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentDataWithEntity<Target, Translation, AttackDistance, Destination, Velocity>
        {
            [ReadOnly]
            public ComponentDataFromEntity<Translation> TranslationFromEntity;

            public void Execute(Entity entity, int index,
                [ReadOnly] ref Target target,
                [ReadOnly] ref Translation translation,
                [ReadOnly] ref AttackDistance attackDistance,
                ref Destination destination,
                ref Velocity velocity)
            {
                var targetTranslation = TranslationFromEntity[target.Value].Value;
                var distance = math.distance(translation.Value, targetTranslation);

                if (distance < attackDistance.Min || distance > attackDistance.Max)
                {
                    var direction = math.normalizesafe(targetTranslation - translation.Value);
                    destination.LastValue = destination.Value;
                    destination.Value = targetTranslation - direction * attackDistance.Min * math.length(velocity.Value);
                }
                else
                {
                    velocity.Value = float3.zero;
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job
            {
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(this, inputDeps);
        }
    }
}