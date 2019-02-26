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
            ForEach((Entity entity, ref View view, ref Owner owner) =>
            {
                if (EntityManager.HasComponent<Destroy>(owner.Value))
                {
                    PostUpdateCommands.AddComponent(entity, new Destroy());
                }
            }, m_Group);
        }
    }
}