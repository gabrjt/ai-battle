using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class SetOwnedDestroySystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Owner>() },
                None = new[] { ComponentType.Create<Destroy>() }
            });
        }

        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref Owner owner) =>
            {
                if (EntityManager.HasComponent<Destroy>(owner.Value))
                {
                    PostUpdateCommands.AddComponent(entity, new Destroy());
                }
            }, m_Group);
        }
    }
}