using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class SetHealthBarDestroyedSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<HealthBar>() },
                None = new[] { ComponentType.ReadOnly<Destroy>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref HealthBar healthBar) =>
            {
                if (EntityManager.HasComponent<Dead>(healthBar.Owner))
                {
                    PostUpdateCommands.AddComponent(entity, new Destroy());
                }
            }, m_Group);
        }
    }
}