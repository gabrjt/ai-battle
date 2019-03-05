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
#if UNITY_EDITOR
        private InstantiateAICharacterSystem m_InstantiateAICharacterSystem;
        private bool m_InstantiateAICharacterSystemEnabled;
#endif

        private void Start()
        {
            m_EntityManager = World.Active.GetExistingManager<EntityManager>();
            m_Archetype = m_EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<KillAllCharacters>());
#if UNITY_EDITOR
            m_InstantiateAICharacterSystem = World.Active.GetExistingManager<InstantiateAICharacterSystem>();
            m_InstantiateAICharacterSystemEnabled = m_InstantiateAICharacterSystem != null ? m_InstantiateAICharacterSystem.Enabled : false;
#endif
        }

        public void KillAllCharacters()
        {
#if UNITY_EDITOR
            if (m_InstantiateAICharacterSystemEnabled)
            {
                m_InstantiateAICharacterSystemEnabled = m_InstantiateAICharacterSystem.Enabled = false;
                Invoke("EnableInstantiateAICharacterSystem", 5);
            }
#endif
            World.Active.GetExistingManager<EventCommandBufferSystem>().CreateCommandBuffer().CreateEntity(m_Archetype);
        }

#if UNITY_EDITOR

        private void EnableInstantiateAICharacterSystem()
        {
            m_InstantiateAICharacterSystemEnabled = m_InstantiateAICharacterSystem.Enabled = true;
        }

#endif
    }
}