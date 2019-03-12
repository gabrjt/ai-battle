using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ChargeSystem : JobComponentSystem
    {
        [BurstCompile]
        [RequireComponentTag(typeof(Motion))]
        [ExcludeComponent(typeof(Dead), typeof(Walking))]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<ChargeSpeedModifier>
        {
            [NativeDisableParallelForRestriction] public NativeArray<Entity> EntityArray;
            [NativeDisableParallelForRestriction] public NativeArray<@bool> AddArray;
            [NativeDisableParallelForRestriction] public NativeArray<@bool> RemoveArray;
            [ReadOnly] public ComponentDataFromEntity<Target> TargetFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Charging> ChargingFromEntity;

            public void Execute(Entity entity, int index, [ReadOnly] ref ChargeSpeedModifier chargeSpeedModifier)
            {
                var hasTarget = TargetFromEntity.Exists(entity);
                var hasCharging = ChargingFromEntity.Exists(entity);

                EntityArray[index] = entity;
                AddArray[index] = hasTarget && !hasCharging;
                RemoveArray[index] = !hasTarget && hasCharging;
            }
        }

        private struct WalkingJob : IJobParallelFor
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> EntityArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<@bool> AddArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<@bool> RemoveArray;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                if (AddArray[index])
                {
                    CommandBuffer.AddComponent(m_ThreadIndex, EntityArray[index], new Charging());
                }
                else if (RemoveArray[index])
                {
                    CommandBuffer.RemoveComponent<Charging>(m_ThreadIndex, EntityArray[index]);
                }
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Motion>(), ComponentType.ReadOnly<ChargeSpeedModifier>() },
                None = new[] { ComponentType.ReadOnly<Dead>(), ComponentType.ReadOnly<Walking>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var length = m_Group.CalculateLength();
            var entityArray = new NativeArray<Entity>(length, Allocator.TempJob);
            var addArray = new NativeArray<@bool>(length, Allocator.TempJob);
            var removeArray = new NativeArray<@bool>(length, Allocator.TempJob);
            var commandBufferSystem = World.Active.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            inputDeps = new ProcessJob
            {
                EntityArray = entityArray,
                AddArray = addArray,
                RemoveArray = removeArray,
                TargetFromEntity = GetComponentDataFromEntity<Target>(true),
                ChargingFromEntity = GetComponentDataFromEntity<Charging>(true)
            }.Schedule(this, inputDeps);

            inputDeps = new WalkingJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityArray = entityArray,
                AddArray = addArray,
                RemoveArray = removeArray
            }.Schedule(length, 64, inputDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}