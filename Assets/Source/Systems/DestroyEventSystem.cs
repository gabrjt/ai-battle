using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public class DestroyEventSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Event>() }
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.DestroyEntity(m_Group);
        }
    }
}