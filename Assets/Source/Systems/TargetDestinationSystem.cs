using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class TargetDestinationSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentData<Target, Destination>
        {
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;

            public void Execute([ReadOnly] ref Target target, ref Destination destination)
            {
                destination.Value = TranslationFromEntity[target.Value].Value;
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