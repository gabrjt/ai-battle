using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateBefore(typeof(FacingTargetSystem))]
    public class CleanFacingTargetSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<FacingTarget>().WithNone<Target>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            EntityManager.RemoveComponent(m_Group, ComponentType.ReadWrite<FacingTarget>());
        }
    }
}