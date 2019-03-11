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
        private List<GameObject> m_KnightList;
        private List<GameObject> m_OrcWolfRiderList;
        private List<GameObject> m_SkeletonList;
        private ComponentGroup m_ViewGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_KnightPool = new Queue<GameObject>();
            m_OrcWolfRiderPool = new Queue<GameObject>();
            m_SkeletonPool = new Queue<GameObject>();
            m_KnightList = new List<GameObject>();
            m_OrcWolfRiderList = new List<GameObject>();
            m_SkeletonList = new List<GameObject>();
            m_ViewGroup = Entities.WithAll<View>().WithAny<Knight, OrcWolfRider, Skeleton>().WithNone<Parent>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_ViewGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            var knightList = new NativeList<Entity>(Allocator.TempJob);
            var orcWolfRiderList = new NativeList<Entity>(Allocator.TempJob);
            var skeletonList = new NativeList<Entity>(Allocator.TempJob);
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
                    knightList.AddRange(entityArray);
                }
                else if (chunk.Has(orcWolfRiderType))
                {
                    orcWolfRiderList.AddRange(entityArray);
                }
                else if (chunk.Has(skeletonType))
                {
                    skeletonList.AddRange(entityArray);
                }
            }

            chunkArray.Dispose();

            AddToPool(m_KnightPool, knightList);
            AddToPool(m_OrcWolfRiderPool, orcWolfRiderList);
            AddToPool(m_SkeletonPool, skeletonList);
            knightList.Dispose();
            orcWolfRiderList.Dispose();
            skeletonList.Dispose();
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
    }
}