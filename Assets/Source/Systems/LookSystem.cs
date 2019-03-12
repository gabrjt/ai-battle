using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class LookSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(Target), typeof(Dead))]
        private struct LookToDestinationJob : IJobProcessComponentData<Destination, Translation, RotationSpeed, Rotation>
        {
            [ReadOnly] public float DeltaTime;

            public void Execute([ReadOnly] ref Destination destination, [ReadOnly] ref Translation translation, [ReadOnly] ref RotationSpeed rotationSpeed, ref Rotation rotation)
            {
                rotation.Value = math.slerp(rotation.Value, quaternion.LookRotationSafe(math.normalizesafe(destination.Value - translation.Value), math.up()),
                    rotationSpeed.Value * DeltaTime);
            }
        }

        [BurstCompile]
        [ExcludeComponent(typeof(Dead))]
        private struct LookToTargetJob : IJobProcessComponentData<Target, Translation, RotationSpeed, Rotation>
        {
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
            [ReadOnly] public float DeltaTime;

            public void Execute([ReadOnly] ref Target target, [ReadOnly] ref Translation translation, [ReadOnly] ref RotationSpeed rotationSpeed, ref Rotation rotation)
            {
                if (!TranslationFromEntity.Exists(target.Value)) return;

                rotation.Value = math.slerp(rotation.Value, quaternion.LookRotationSafe(math.normalizesafe(TranslationFromEntity[target.Value].Value - translation.Value), math.up()),
                    rotationSpeed.Value * DeltaTime);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            inputDeps = new LookToDestinationJob
            {
                DeltaTime = Time.deltaTime
            }.Schedule(this, inputDeps);

            inputDeps = new LookToTargetJob
            {
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(),
                DeltaTime = Time.deltaTime,
            }.Schedule(this, inputDeps);

            return inputDeps;
        }
    }
}