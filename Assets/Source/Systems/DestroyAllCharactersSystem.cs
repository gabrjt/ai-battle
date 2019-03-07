using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    public class DestroyAllCharactersSystem : ComponentSystem
    {
        private ComponentGroup m_CharacterGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_CharacterGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<Destroyed>() }
            });

            RequireSingletonForUpdate<DestroyAllCharacters>();
        }

        protected override void OnUpdate()
        {
            EntityManager.AddComponent(m_CharacterGroup, ComponentType.ReadWrite<Destroy>());
        }
    }
}
