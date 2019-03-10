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
        [ExcludeComponent(typeof(Target))]
        private struct LookToDestinationJob : IJobProcessComponentData<Destination, Translation, Rotation>
        {
            [ReadOnly] public float DeltaTime;

            public void Execute([ReadOnly] ref Destination destination, [ReadOnly] ref Translation translation, ref Rotation rotation)
            {
                rotation.Value = math.slerp(rotation.Value, quaternion.LookRotationSafe(math.normalizesafe(destination.Value - translation.Value), math.up()), DeltaTime);
            }
        }

        [BurstCompile]
        private struct LookToTargetJob : IJobProcessComponentData<Target, Translation, Rotation>
        {
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
            [ReadOnly] public float DeltaTime;

            public void Execute([ReadOnly] ref Target target, [ReadOnly] ref Translation translation, ref Rotation rotation)
            {
                if (!TranslationFromEntity.Exists(target.Value)) return;

                rotation.Value = math.slerp(rotation.Value, quaternion.LookRotationSafe(math.normalizesafe(TranslationFromEntity[target.Value].Value - translation.Value), math.up()), DeltaTime);
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