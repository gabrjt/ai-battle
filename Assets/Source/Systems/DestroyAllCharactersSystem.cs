using Game.Components;
using Unity.Entities;

namespace Game.Systems
{
    public class DestroyAllCharactersSystem : ComponentSystem
    {
        private ComponentGroup m_CharacterGroup;
        private ComponentGroup m_EventGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_CharacterGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<Destroyed>() }
            });

            m_EventGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<DestroyAllCharacters>() }
            });

            RequireForUpdate(m_EventGroup);
        }

        protected override void OnUpdate()
        {
            ForEach((Entity entity) =>
            {
                PostUpdateCommands.AddComponent(entity, new Destroy());
            }, m_CharacterGroup);
        }
    }
}