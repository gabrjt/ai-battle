using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(ProcessVelocitySystem))]
    [UpdateBefore(typeof(MoveSystem))]
    public class TargetDestinationSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Target, Translation, AttackDistance, Velocity, Destination>
        {
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref Target target, [ReadOnly] ref Translation translation, [ReadOnly] ref AttackDistance attackDistance, ref Velocity velocity, ref Destination destination)
            {
                var targetTranslation = TranslationFromEntity[target.Value].Value;
                var distance = math.distance(translation.Value, targetTranslation);

                if (distance < attackDistance.Min || distance > attackDistance.Max)
                {
                    var direction = math.normalizesafe(targetTranslation - translation.Value);
                    destination.Value = targetTranslation - direction * attackDistance.Min;
                }
                else
                {
                    velocity.Value = float3.zero;
                }
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new ProcessJob
            {
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true)
            }.Schedule(this, inputDeps);
        }
    }
}