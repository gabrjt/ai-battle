using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(DisengageSystem))]
    public class AttackSystem : JobComponentSystem
    {
        private struct AttackingData
        {
            public Entity Entity;
            public Entity Target;
            public Attacking Attacking;
            public float Damage;
        }

        [BurstCompile]
        [RequireComponentTag(typeof(TargetInRange))]
        [ExcludeComponent(typeof(Attacking), typeof(Cooldown), typeof(Dying))]
        private struct ConsolidateJob : IJobProcessComponentDataWithEntity<Target, AttackDamage, AttackDuration, AttackSpeed>
        {
            public NativeQueue<AttackingData>.Concurrent AddQueue;

            public void Execute(Entity entity, int index, [ReadOnly] ref Target target, [ReadOnly] ref AttackDamage attackDamage, [ReadOnly] ref AttackDuration attackDuration, [ReadOnly] ref AttackSpeed attackSpeed)
            {
                AddQueue.Enqueue(new AttackingData
                {
                    Entity = entity,
                    Target = target.Value,
                    Attacking = new Attacking
                    {
                        Duration = attackDuration.Value / attackSpeed.Value
                    },
                    Damage = attackDamage.Value
                });
            }
        }

        private struct AddJob : IJob
        {
            public NativeQueue<AttackingData> AddQueue;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public EntityArchetype Archetype;

            public void Execute()
            {
                while (AddQueue.TryDequeue(out var data))
                {
                    CommandBuffer.AddComponent(data.Entity, data.Attacking);

                    var damaged = CommandBuffer.CreateEntity(Archetype);
                    CommandBuffer.SetComponent(damaged, new Damaged
                    {
                        This = data.Entity,
                        Other = data.Target,
                        Value = data.Damage
                    });
                }
            }
        }

        [BurstCompile]
        private struct ProcessJob : IJobProcessComponentDataWithEntity<Attacking>
        {
            public NativeQueue<Entity>.Concurrent ProcessedQueue;
            [ReadOnly] public float DeltaTime;

            public void Execute(Entity entity, int index, ref Attacking attacking)
            {
                attacking.Duration -= DeltaTime;

                if (attacking.Duration > 0) return;

                ProcessedQueue.Enqueue(entity);
            }
        }

        private struct RemoveJob : IJob
        {
            public NativeQueue<Entity> ProcessedQueue;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (ProcessedQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<Attacking>(entity);
                }
            }
        }

        private EntityArchetype m_Archetype;
        private NativeQueue<AttackingData> m_AddQueue;
        private NativeQueue<Entity> m_ProcessedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<Damaged>());
            m_AddQueue = new NativeQueue<AttackingData>(Allocator.Persistent);
            m_ProcessedQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            var consolidateDeps = new ConsolidateJob
            {
                AddQueue = m_AddQueue.ToConcurrent()
            }.Schedule(this, inputDeps);

            var addDeps = new AddJob
            {
                AddQueue = m_AddQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer(),
                Archetype = m_Archetype
            }.Schedule(consolidateDeps);

            var processDeps = new ProcessJob
            {
                ProcessedQueue = m_ProcessedQueue.ToConcurrent(),
                DeltaTime = Time.deltaTime
            }.Schedule(this, inputDeps);

            var removeDeps = new RemoveJob
            {
                ProcessedQueue = m_ProcessedQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer()
            }.Schedule(processDeps);

            inputDeps = JobHandle.CombineDependencies(addDeps, removeDeps);

            commandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_AddQueue.IsCreated)
            {
                m_AddQueue.Dispose();
            }

            if (m_ProcessedQueue.IsCreated)
            {
                m_ProcessedQueue.Dispose();
            }
        }
    }
}