using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class SetViewDestroyedSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<View>() },
                None = new[] { ComponentType.ReadOnly<Destroy>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref View view) =>
            {
                if (EntityManager.HasComponent<Destroy>(view.Owner))
                {
                    PostUpdateCommands.AddComponent(entity, new Destroy());
                }
            }, m_Group);
        }
    }
}