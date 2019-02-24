using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateAfter(typeof(EndFrameBarrier))]
    public class CleanupEventsSystem : ComponentSystem
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
        }
    }
}