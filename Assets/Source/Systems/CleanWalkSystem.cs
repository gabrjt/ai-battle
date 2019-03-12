using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateBefore(typeof(WalkSystem))]
    public class CleanWalkSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<Walking>().WithNone<Motion>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            EntityManager.RemoveComponent(m_Group, ComponentType.ReadWrite<Walking>());
        }
    }
}