using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class WalkSystem : ComponentSystem
    {
        private ComponentGroup m_AddGroup;
        private ComponentGroup m_RemoveGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Motion>() },
                None = new[] { ComponentType.ReadWrite<Walking>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            });

            m_RemoveGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Walking>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Walking>(), ComponentType.ReadOnly<Target>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Walking>() },
                None = new[] { ComponentType.ReadOnly<Motion>() }
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_AddGroup, ComponentType.ReadWrite<Walking>());
            EntityManager.RemoveComponent(m_RemoveGroup, ComponentType.ReadWrite<Walking>());
        }
    }
}