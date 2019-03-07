using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(FixedSimulationLogic))]
    public class WalkVelocitySystem : JobComponentSystem
    {
        [BurstCompile]
        private struct Job : IJobProcessComponentData<WalkSpeed, MovementDirection, Velocity>
        {
            public void Execute([ReadOnly] ref WalkSpeed walkSpeed, [ReadOnly] ref MovementDirection movementDirection, ref Velocity velocity)
            {
                velocity.Value = walkSpeed.Value * movementDirection.Value;
            }
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return new Job { }.Schedule(this, inputDeps);
        }
    }
}