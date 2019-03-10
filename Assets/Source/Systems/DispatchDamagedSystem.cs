using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateBefore(typeof(DamageSystem))]
    public class DispatchDamagedSystem : ComponentSystem
    {
        private struct DamagedDispatched : ISystemStateComponentData { }

        [BurstCompile]
        [ExcludeComponent(typeof(DamagedDispatched))]
        private struct ProcessDamagedJob : IJobProcessComponentDataWithEntity<Target, AttackAnimationDuration, AttackSpeed, AttackDamage, AttackDuration>
        {
            public NativeQueue<Damaged>.Concurrent DamagedQueue;

            public void Execute(Entity entity, int index,
               [ReadOnly] ref Target target,
               [ReadOnly] ref AttackAnimationDuration attackAnimationDuration,
               [ReadOnly] ref AttackSpeed attackSpeed,
               [ReadOnly] ref AttackDamage attackDamage,
               ref AttackDuration attackDuration)
            {
                if (attackDuration.Value > attackAnimationDuration.Value / attackSpeed.Value * 0.5f) return;

                DamagedQueue.Enqueue(new Damaged
                {
                    This = entity,
                    Other = target.Value,
                    Value = attackDamage.Value
                });
            }
        }

        private ComponentGroup m_Group;
        private EntityArchetype m_Archetype;
        private NativeQueue<Damaged> m_DamagedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<DamagedDispatched>().WithNone<AttackDuration>().ToComponentGroup();
            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Damaged>());
            m_DamagedQueue = new NativeQueue<Damaged>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            if (m_Group.CalculateLength() > 0)
            {
                EntityManager.RemoveComponent(m_Group, ComponentType.ReadWrite<DamagedDispatched>());
            }

            new ProcessDamagedJob
            {
                DamagedQueue = m_DamagedQueue.ToConcurrent()
            }.Schedule(this).Complete();

            while (m_DamagedQueue.TryDequeue(out var component))
            {
                if (!(EntityManager.Exists(component.This) && EntityManager.Exists(component.Other))) continue;

                var damaged = PostUpdateCommands.CreateEntity(m_Archetype);
                PostUpdateCommands.SetComponent(damaged, component);

                PostUpdateCommands.AddComponent(component.This, new DamagedDispatched());
            }
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_DamagedQueue.IsCreated)
            {
                m_DamagedQueue.Dispose();
            }
        }
    }
}