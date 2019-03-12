using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateBefore(typeof(ChargeSystem))]
    public class CleanChargeSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = Entities.WithAll<Charging>().WithNone<Motion>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            EntityManager.RemoveComponent(m_Group, ComponentType.ReadWrite<Charging>());
        }
    }
}