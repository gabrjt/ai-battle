using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class DestroyAllCharactersSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<Destroy>() }
            });

            RequireSingletonForUpdate<DestroyAllCharacters>();
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_Group, ComponentType.ReadWrite<Destroy>());
            EntityManager.AddComponent(m_Group, ComponentType.ReadWrite<Disabled>());
        }
    }
}
