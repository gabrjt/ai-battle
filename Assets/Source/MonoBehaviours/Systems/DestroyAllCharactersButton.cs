﻿using Game.Components;
using Game.Systems;
using Unity.Entities;
using UnityEngine;

namespace Game.MonoBehaviours
{
    public class DestroyAllCharactersButton : MonoBehaviour
    {
        private EntityManager m_EntityManager;
        private EntityArchetype m_Archetype;
        private InstantiateAICharacterSystem m_InstantiateAICharacterSystem;
        private bool m_InstantiateAICharacterSystemEnabled;

        private void Start()
        {
            m_EntityManager = World.Active.GetExistingManager<EntityManager>();
            m_Archetype = m_EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<DestroyAllCharacters>());
            m_InstantiateAICharacterSystem = World.Active.GetExistingManager<InstantiateAICharacterSystem>();
            m_InstantiateAICharacterSystemEnabled = m_InstantiateAICharacterSystem != null ? m_InstantiateAICharacterSystem.Enabled : false;
        }

        public void DestroyAllCharacters()
        {
            if (m_InstantiateAICharacterSystemEnabled)
            {
                m_InstantiateAICharacterSystemEnabled = m_InstantiateAICharacterSystem.Enabled = false;
                Invoke("EnableInstantiateAICharacterSystem", 5);
            }
            World.Active.GetExistingManager<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer().CreateEntity(m_Archetype);
        }

        private void EnableInstantiateAICharacterSystem()
        {
            m_InstantiateAICharacterSystemEnabled = m_InstantiateAICharacterSystem.Enabled = true;
        }
    }
}