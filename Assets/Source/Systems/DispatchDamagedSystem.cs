using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
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

        private struct DispatchDamagedJob : IJob
        {
            public NativeQueue<Damaged> DamagedQueue;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public EntityArchetype Archetype;

            public void Execute()
            {
                while (DamagedQueue.TryDequeue(out var component))
                {
                    var damaged = CommandBuffer.CreateEntity(Archetype);
                    CommandBuffer.SetComponent(damaged, component);

                    CommandBuffer.AddComponent(component.This, new DamagedDispatched());
                }
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
            EntityManager.RemoveComponent(m_Group, ComponentType.ReadWrite<DamagedDispatched>());

            var commandBufferSystem = World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>();

            var inputDeps = new ProcessDamagedJob
            {
                DamagedQueue = m_DamagedQueue.ToConcurrent()
            }.Schedule(this);

            inputDeps = new DispatchDamagedJob
            {
                DamagedQueue = m_DamagedQueue,
                CommandBuffer = commandBufferSystem.CreateCommandBuffer(),
                Archetype = m_Archetype
            }.Schedule(inputDeps);

            inputDeps.Complete();
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