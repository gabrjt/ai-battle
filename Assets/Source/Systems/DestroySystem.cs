using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateAfter(typeof(EndFrameBarrier))]
    public class DestroySystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>() }
            });
        }

        protected override void OnUpdate()
        {
            EntityManager.DestroyEntity(m_Group);
        }
    }
}