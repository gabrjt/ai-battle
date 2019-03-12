using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class AttackSystem : JobComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(AttackDuration))]
        private struct ConsolidateAttackDurationJob : IJobProcessComponentDataWithEntity<Attacking, AttackAnimationDuration, AttackSpeed>
        {
            [NativeDisableParallelForRestriction] public NativeArray<Entity> EntityArray;
            [NativeDisableParallelForRestriction] public NativeArray<AttackDuration> AttackDurationArray;

            public void Execute(Entity entity, int index, [ReadOnly] ref Attacking attacking, [ReadOnly] ref AttackAnimationDuration attackAnimationDuration, [ReadOnly] ref AttackSpeed attackSpeed)
            {
                EntityArray[index] = entity;
                AttackDurationArray[index] = new AttackDuration { Value = attackAnimationDuration.Value / attackSpeed.Value };
            }
        }

        private struct AddAttackDurationJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> EntityArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<AttackDuration> AttackDurationArray;
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                CommandBuffer.AddComponent(m_ThreadIndex, EntityArray[index], AttackDurationArray[index]);
            }
        }

        private ComponentGroup m_RemoveAttackingGroup;
        private ComponentGroup m_AddAttackingGroup;
        private ComponentGroup m_AddAttackDurationGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_RemoveAttackingGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Attacking>(), ComponentType.ReadOnly<Dead>() }
            });
            m_AddAttackingGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<TargetInRange>(), ComponentType.ReadOnly<FacingTarget>(), ComponentType.ReadOnly<Target>() },
                None = new[] { ComponentType.ReadWrite<Attacking>(), ComponentType.ReadOnly<Cooldown>(), ComponentType.ReadOnly<Dead>() }
            });
            m_AddAttackDurationGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Attacking>() },
                None = new[] { ComponentType.ReadOnly<AttackDuration>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_RemoveAttackingGroup.CalculateLength() > 0)
            {
                EntityManager.RemoveComponent(m_RemoveAttackingGroup, ComponentType.ReadWrite<AttackDuration>());
                EntityManager.RemoveComponent(m_RemoveAttackingGroup, ComponentType.ReadWrite<Attacking>());
            }

            EntityManager.AddComponent(m_AddAttackingGroup, ComponentType.ReadWrite<Attacking>());

            var addAttackDurationGroupLength = m_AddAttackDurationGroup.CalculateLength();
            if (addAttackDurationGroupLength > 0)
            {
                var entityArray = new NativeArray<Entity>(addAttackDurationGroupLength, Allocator.TempJob);
                var attackDurationArray = new NativeArray<AttackDuration>(addAttackDurationGroupLength, Allocator.TempJob);
                var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

                inputDeps = new ConsolidateAttackDurationJob
                {
                    EntityArray = entityArray,
                    AttackDurationArray = attackDurationArray,
                }.Schedule(this);

                inputDeps = new AddAttackDurationJob
                {
                    EntityArray = entityArray,
                    AttackDurationArray = attackDurationArray,
                    CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent()
                }.Schedule(addAttackDurationGroupLength, 64, inputDeps);

                commandBufferSystem.AddJobHandleForProducer(inputDeps);
            }

            return inputDeps;
        }
    }
}