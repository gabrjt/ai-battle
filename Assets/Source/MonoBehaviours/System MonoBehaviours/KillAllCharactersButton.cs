using Game.Components;
using Game.Systems;
using Unity.Entities;
using UnityEngine;

namespace Game.MonoBehaviours
{
    public class KillAllCharactersButton : MonoBehaviour
    {
        private EntityArchetype m_Archetype;

        private EntityManager m_EntityManager;

        private void Start()
        {
            m_EntityManager = World.Active.GetExistingManager<EntityManager>();

            m_Archetype = m_EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<KillAllCharacters>());
        }

        public void KillAllCharacters()
        {
            World.Active.GetExistingManager<EventBarrier>().CreateCommandBuffer().CreateEntity(m_Archetype);
        }
    }
}