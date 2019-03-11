using Game.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameObjectPoolGroup))]
    public class ViewPoolSystem : ComponentSystem
    {
        internal Queue<GameObject> m_KnightPool;
        internal Queue<GameObject> m_OrcWolfRiderPool;
        internal Queue<GameObject> m_SkeletonPool;
        private NativeList<Entity> m_KnightList;
        private NativeList<Entity> m_OrcWolfRiderList;
        private NativeList<Entity> m_SkeletonList;
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_KnightPool = new Queue<GameObject>();
            m_OrcWolfRiderPool = new Queue<GameObject>();
            m_SkeletonPool = new Queue<GameObject>();
            m_KnightList = new NativeList<Entity>(Allocator.Persistent);
            m_OrcWolfRiderList = new NativeList<Entity>(Allocator.Persistent);
            m_SkeletonList = new NativeList<Entity>(Allocator.Persistent);
            m_Group = Entities.WithAll<View>().WithAny<Knight, OrcWolfRider, Skeleton>().WithNone<Parent>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var knightType = GetArchetypeChunkComponentType<Knight>(true);
            var orcWolfRiderType = GetArchetypeChunkComponentType<OrcWolfRider>(true);
            var skeletonType = GetArchetypeChunkComponentType<Skeleton>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                if (chunk.Has(knightType))
                {
                    m_KnightList.AddRange(entityArray);
                }
                else if (chunk.Has(orcWolfRiderType))
                {
                    m_OrcWolfRiderList.AddRange(entityArray);
                }
                else if (chunk.Has(skeletonType))
                {
                    m_SkeletonList.AddRange(entityArray);
                }
            }

            chunkArray.Dispose();
            AddToPool(m_KnightPool, m_KnightList);
            AddToPool(m_OrcWolfRiderPool, m_OrcWolfRiderList);
            AddToPool(m_SkeletonPool, m_SkeletonList);
            m_KnightList.Clear();
            m_OrcWolfRiderList.Clear();
            m_SkeletonList.Clear();
        }

        private void AddToPool(Queue<GameObject> pool, NativeArray<Entity> entityArray)
        {
            for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                AddToPool(pool, entityArray[entityIndex]);
            }
        }

        private void AddToPool(Queue<GameObject> pool, Entity entity)
        {
            var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;
            gameObject.SetActive(false);
            pool.Enqueue(gameObject);
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_KnightList.IsCreated)
            {
                m_KnightList.Dispose();
            }

            if (m_OrcWolfRiderList.IsCreated)
            {
                m_OrcWolfRiderList.Dispose();
            }

            if (m_SkeletonList.IsCreated)
            {
                m_SkeletonList.Dispose();
            }
        }
    }
}