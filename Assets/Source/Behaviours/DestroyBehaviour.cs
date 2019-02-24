using Game.Components;
using Unity.Entities;
using UnityEngine;

namespace Game.Behaviours
{
    [RequireComponent(typeof(GameObjectEntity))]
    public class DestroyBehaviour : MonoBehaviour
    {
        private GameObjectEntity m_GameObjectEntity;

        private void Start()
        {
            m_GameObjectEntity = GetComponent<GameObjectEntity>();
        }

        private void Update()
        {
            var entityManager = m_GameObjectEntity.EntityManager;
            var entity = m_GameObjectEntity.Entity;

            if (!entityManager.HasComponent<Destroy>(entity)) return;

            Destroy(gameObject);
        }
    }
}