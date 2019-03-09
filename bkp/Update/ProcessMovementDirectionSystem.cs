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
    public class ProcessMovementDirectionSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentData<Translation, Destination, MovementDirection>
        {
            public void Execute([ReadOnly] ref Translation translation, [ReadOnly] ref Destination destination, ref MovementDirection movementDirection)
            {
                movementDirection.Value = math.normalizesafe(destination.Value - translation.Value);
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new ProcessJob().Schedule(this, inputDeps);
        }
    }
}