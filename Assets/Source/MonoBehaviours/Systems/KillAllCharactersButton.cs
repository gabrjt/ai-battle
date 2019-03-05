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
        private SpawnAICharacterSystem m_SpawnAICharacterSystem;
        private bool m_DebugSpawnSystemEnabled;
#endif

        private void Start()
        {
            m_EntityManager = World.Active.GetExistingManager<EntityManager>();
            m_Archetype = m_EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<KillAllCharacters>());
#if UNITY_EDITOR
            m_SpawnAICharacterSystem = World.Active.GetExistingManager<SpawnAICharacterSystem>();
            m_DebugSpawnSystemEnabled = m_SpawnAICharacterSystem != null ? m_SpawnAICharacterSystem.Enabled : false;
#endif
        }

        public void KillAllCharacters()
        {
            World.Active.GetExistingManager<EventCommandBufferSystem>().CreateCommandBuffer().CreateEntity(m_Archetype);

#if UNITY_EDITOR
            if (m_DebugSpawnSystemEnabled)
            {
                m_DebugSpawnSystemEnabled = m_SpawnAICharacterSystem.Enabled = false;
                Invoke("EnableSpawnCharacterAISystem", 3);
            }
#endif
        }

#if UNITY_EDITOR

        private void EnableSpawnCharacterAISystem()
        {
            m_DebugSpawnSystemEnabled = m_SpawnAICharacterSystem.Enabled = true;
        }

#endif
    }
}