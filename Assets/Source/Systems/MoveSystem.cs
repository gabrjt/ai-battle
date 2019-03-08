using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class MoveSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct MoveJob : IJobProcessComponentData<Velocity, Translation>
        {
            [ReadOnly] public float DeltaTime;

            public void Execute([ReadOnly] ref Velocity velocity, ref Translation translation)
            {
                translation.Value += velocity.Value * DeltaTime;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new MoveJob
            {
                DeltaTime = Time.deltaTime
            }.Schedule(this, inputDeps);
        }
    }
}