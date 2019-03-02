using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [AlwaysUpdateSystem]
    [UpdateBefore(typeof(EndFrameBarrier))]
    public class EventBarrier : BarrierSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() }
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.DestroyEntity(m_Group);
            base.OnUpdate();
        }
    }
}