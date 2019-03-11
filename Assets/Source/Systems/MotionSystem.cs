using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class MotionSystem : ComponentSystem
    {
        private ComponentGroup m_AddGroup;
        private ComponentGroup m_RemoveGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<MovementSpeed>() },
                None = new[] { ComponentType.ReadWrite<Motion>(), ComponentType.ReadOnly<Dead>() }
            });

            m_RemoveGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Motion>() },
                None = new[] { ComponentType.ReadOnly<Destination>() }
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_AddGroup, ComponentType.ReadWrite<Motion>());
            EntityManager.RemoveComponent(m_RemoveGroup, ComponentType.ReadWrite<Motion>());
        }
    }
}