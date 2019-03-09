using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ProcessVelocitySystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentData<MovementDirection, MovementSpeed, Velocity>
        {
            public void Execute([ReadOnly] ref MovementDirection movementDirection, [ReadOnly] ref MovementSpeed movementSpeed, ref Velocity velocity)
            {
                velocity.Value = movementDirection.Value * movementSpeed.Value;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new ProcessJob().Schedule(this, inputDeps);
        }
    }
}