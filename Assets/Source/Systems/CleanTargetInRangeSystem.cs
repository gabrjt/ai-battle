using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateBefore(typeof(ProcessTargetInRangeSystem))]
    public class CleanTargetInRangeSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<TargetInRange>().WithNone<Target>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            EntityManager.RemoveComponent(m_Group, ComponentType.ReadWrite<TargetInRange>());
        }
    }
}