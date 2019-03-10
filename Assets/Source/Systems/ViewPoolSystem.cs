using Game.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(EntityLifecycleGroup))]
    [UpdateBefore(typeof(InstantiateViewSystem))]
    public class ViewPoolSystem : ComponentSystem
    {
        internal Queue<GameObject> m_KnightPool;
        internal Queue<GameObject> m_OrcWolfRiderPool;
        internal Queue<GameObject> m_SkeletonPool;
        private ComponentGroup m_KnightGroup;
        private ComponentGroup m_OrcWolfRiderGroup;
        private ComponentGroup m_SkeletonGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_KnightPool = new Queue<GameObject>();
            m_OrcWolfRiderPool = new Queue<GameObject>();
            m_SkeletonPool = new Queue<GameObject>();
            m_KnightGroup = Entities.WithAll<View, Knight>().WithNone<Parent>().ToComponentGroup();
            m_OrcWolfRiderGroup = Entities.WithAll<View, OrcWolfRider>().WithNone<Parent>().ToComponentGroup();
            m_SkeletonGroup = Entities.WithAll<View, Skeleton>().WithNone<Parent>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            AddToPool(m_KnightPool, m_KnightGroup);
            AddToPool(m_OrcWolfRiderPool, m_OrcWolfRiderGroup);
            AddToPool(m_SkeletonPool, m_SkeletonGroup);
        }

        private void AddToPool(Queue<GameObject> pool, ComponentGroup group)
        {
            var entityArray = group.ToEntityArray(Allocator.TempJob);

            for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                var entity = entityArray[entityIndex];
                var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;
                var meshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

                foreach (var meshRenderer in meshRenderers)
                {
                    meshRenderer.enabled = false;
                }

                gameObject.GetComponentInChildren<Animator>().enabled = false;
                gameObject.SetActive(false);
                pool.Enqueue(gameObject);
            }

            entityArray.Dispose();
        }
    }
}