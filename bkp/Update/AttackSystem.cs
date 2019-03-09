using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(DisengageSystem))]
    public class AttackSystem : ComponentSystem
    {
        [BurstCompile]
        [ExcludeComponent(typeof(AttackDuration))]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<Target, AttackDamage, AttackAnimationDuration, AttackSpeed>
        {
            [NativeDisableParallelForRestriction] public NativeArray<Entity> EntityArray;
            [NativeDisableParallelForRestriction] public NativeArray<Entity> TargetArray;
            [NativeDisableParallelForRestriction] public NativeArray<float> DamageArray;
            [NativeDisableParallelForRestriction] public NativeArray<AttackDuration> AttackDurationArray;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(Entity entity, int index, [ReadOnly] ref Target target, [ReadOnly] ref AttackDamage attackDamage, [ReadOnly] ref AttackAnimationDuration attackDuration, [ReadOnly] ref AttackSpeed attackSpeed)
            {
                EntityArray[m_ThreadIndex] = entity;
                TargetArray[m_ThreadIndex] = target.Value;
                DamageArray[m_ThreadIndex] = attackDamage.Value;
                AttackDurationArray[m_ThreadIndex] = new AttackDuration { Value = attackDuration.Value / attackSpeed.Value };
            }
        }

        [ExcludeComponent(typeof(AttackDuration))]
        private struct ApplyJob : IJobParallelFor
        {
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> EntityArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<Entity> TargetArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<float> DamageArray;
            [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<AttackDuration> AttackDurationArray;
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public EntityArchetype Archetype;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(int index)
            {
                var target = TargetArray[m_ThreadIndex];
                var damage = DamageArray[m_ThreadIndex];
                var entity = EntityArray[m_ThreadIndex];

                CommandBuffer.AddComponent(m_ThreadIndex, entity, AttackDurationArray[m_ThreadIndex]);

                var damaged = CommandBuffer.CreateEntity(m_ThreadIndex, Archetype);
                CommandBuffer.SetComponent(m_ThreadIndex, damaged, new Damaged
                {
                    This = entity,
                    Other = target,
                    Value = damage
                });
            }
        }

        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<AttackDuration>
        {
            public NativeQueue<Entity>.Concurrent ProcessQueue;
            [ReadOnly] public float DeltaTime;

            public void Execute(Entity entity, int index, ref AttackDuration attackDuration)
            {
                attackDuration.Value -= DeltaTime;

                if (attackDuration.Value > 0) return;

                ProcessQueue.Enqueue(entity);
            }
        }

        private EntityArchetype m_Archetype;
        private NativeQueue<Entity> m_ProcessedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<Damaged>());
            m_ProcessedQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            new ProcessJob
            {
                ProcessQueue = m_ProcessedQueue.ToConcurrent(),
                DeltaTime = Time.deltaTime
            }.Schedule(this).Complete();

            if (m_ProcessedQueue.Count > 0)
            {
                var removeArray = new NativeArray<Entity>(m_ProcessedQueue.Count, Allocator.TempJob);
                var removeCount = 0;

                while (m_ProcessedQueue.TryDequeue(out var entity))
                {
                    removeArray[removeCount++] = entity;
                }

                EntityManager.RemoveComponent(removeArray, ComponentType.ReadWrite<Attacking>());
                EntityManager.RemoveComponent(removeArray, ComponentType.ReadWrite<AttackDuration>());

                removeArray.Dispose();
            }

            EntityManager.AddComponent(Entities.WithAll<TargetInRange>().WithNone<Attacking, Cooldown, Dying>().ToComponentGroup(), ComponentType.ReadWrite<Attacking>());

            var attackGroup = Entities.WithAll<Attacking, AttackAnimationDuration, AttackSpeed, AttackDamage, Target>().WithNone<AttackDuration>().ToComponentGroup();
            var attackGroupLength = attackGroup.CalculateLength();

            if (attackGroupLength > 0)
            {
                var commandBuffer = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer();
                var entityArray = new NativeArray<Entity>(attackGroupLength, Allocator.TempJob);
                var targetArray = new NativeArray<Entity>(attackGroupLength, Allocator.TempJob);
                var damageArray = new NativeArray<float>(attackGroupLength, Allocator.TempJob);
                var attackDurationArray = new NativeArray<AttackDuration>(attackGroupLength, Allocator.TempJob);

                var consolidateDeps = new ConsolidateJob
                {
                    EntityArray = entityArray,
                    TargetArray = targetArray,
                    DamageArray = damageArray,
                    AttackDurationArray = attackDurationArray
                }.Schedule(this);

                consolidateDeps = new ApplyJob
                {
                    EntityArray = entityArray,
                    TargetArray = targetArray,
                    DamageArray = damageArray,
                    AttackDurationArray = attackDurationArray,
                    CommandBuffer = commandBuffer.ToConcurrent(),
                    Archetype = m_Archetype
                }.Schedule(attackGroupLength, 64, consolidateDeps);

                consolidateDeps.Complete();

                EntityManager.CompleteAllJobs();
                commandBuffer.Playback(EntityManager);
            }
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_ProcessedQueue.IsCreated)
            {
                m_ProcessedQueue.Dispose();
            }
        }
    }
}