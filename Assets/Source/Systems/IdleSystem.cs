using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class IdleSystem : ComponentSystem
    {
        private ComponentGroup m_ProcessGroup;
        private ComponentGroup m_AddGroup;
        private ComponentGroup m_RemoveGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_ProcessGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Idle>() }
            });

            m_AddGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<Idle>(), ComponentType.ReadOnly<Dead>() }
            });

            m_RemoveGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Idle>() },
                Any = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>() }
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_AddGroup, ComponentType.ReadWrite<Idle>());
            EntityManager.RemoveComponent(m_RemoveGroup, ComponentType.ReadWrite<Idle>());
        }
    }
}