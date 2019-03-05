using Game.Components;
using Game.Systems;
using Unity.Entities;
using UnityEngine;

namespace Game.MonoBehaviours
{
    public class KillAllCharactersButton : MonoBehaviour
    {
        private EntityManager m_EntityManager;
        private EntityArchetype m_Archetype;

        private void Start()
        {
            m_EntityManager = World.Active.GetExistingManager<EntityManager>();
            m_Archetype = m_EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<KillAllCharacters>());
        }

        public void KillAllCharacters()
        {
            World.Active.GetExistingManager<EventCommandBufferSystem>().CreateCommandBuffer().CreateEntity(m_Archetype);
        }
    }
}