using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class ChargeSystem : ComponentSystem
    {
        private ComponentGroup m_AddGroup;
        private ComponentGroup m_RemoveGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Motion>() },
                None = new[] { ComponentType.ReadWrite<Charging>(), ComponentType.ReadOnly<Walking>(), ComponentType.ReadOnly<Attacking>(), ComponentType.ReadOnly<Dead>() }
            });

            m_RemoveGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Charging>(), ComponentType.ReadOnly<TargetInRange>() }
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_AddGroup, ComponentType.ReadWrite<Charging>());
            EntityManager.RemoveComponent(m_RemoveGroup, ComponentType.ReadWrite<Charging>());
        }
    }
}