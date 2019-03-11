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
        private ComponentGroup m_InvisibleGroup;

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
            m_InvisibleGroup = Entities.WithAll<ViewReference>().WithAny<Knight, OrcWolfRider, Skeleton>().WithNone<ViewVisible>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            var viewChunkArray = m_ViewGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            var knightList = new NativeList<Entity>(Allocator.TempJob);
            var orcWolfRiderList = new NativeList<Entity>(Allocator.TempJob);
            var skeletonList = new NativeList<Entity>(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var knightType = GetArchetypeChunkComponentType<Knight>(true);
            var orcWolfRiderType = GetArchetypeChunkComponentType<OrcWolfRider>(true);
            var skeletonType = GetArchetypeChunkComponentType<Skeleton>(true);

            for (var chunkIndex = 0; chunkIndex < viewChunkArray.Length; chunkIndex++)
            {
                var chunk = viewChunkArray[chunkIndex];
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

            viewChunkArray.Dispose();
            AddToPool(m_KnightPool, knightList);
            AddToPool(m_OrcWolfRiderPool, orcWolfRiderList);
            AddToPool(m_SkeletonPool, skeletonList);
            knightList.Dispose();
            orcWolfRiderList.Dispose();
            skeletonList.Dispose();

            var invisibleChunkArray = m_InvisibleGroup.CreateArchetypeChunkArray(Allocator.TempJob);
            entityType = GetArchetypeChunkEntityType();
            knightType = GetArchetypeChunkComponentType<Knight>(true);
            orcWolfRiderType = GetArchetypeChunkComponentType<OrcWolfRider>(true);
            skeletonType = GetArchetypeChunkComponentType<Skeleton>(true);
            var viewReferenceType = GetArchetypeChunkComponentType<ViewReference>();

            for (var chunkIndex = 0; chunkIndex < invisibleChunkArray.Length; chunkIndex++)
            {
                var chunk = invisibleChunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);
                var viewReferenceArray = chunk.GetNativeArray(viewReferenceType);

                if (chunk.Has(knightType))
                {
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        ApplyToPool(m_KnightList, entityArray[entityIndex], viewReferenceArray[entityIndex].Value);
                    }
                }
                else if (chunk.Has(orcWolfRiderType))
                {
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        ApplyToPool(m_OrcWolfRiderList, entityArray[entityIndex], viewReferenceArray[entityIndex].Value);
                    }
                }
                if (chunk.Has(skeletonType))
                {
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        ApplyToPool(m_SkeletonList, entityArray[entityIndex], viewReferenceArray[entityIndex].Value);
                    }
                }
            }

            invisibleChunkArray.Dispose();

            foreach (var gameObject in m_KnightList)
            {
                gameObject.SetActive(false);
                m_KnightPool.Enqueue(gameObject);
            }
            m_KnightList.Clear();

            foreach (var gameObject in m_OrcWolfRiderList)
            {
                gameObject.SetActive(false);
                m_OrcWolfRiderPool.Enqueue(gameObject);
            }
            m_OrcWolfRiderList.Clear();

            foreach (var gameObject in m_SkeletonList)
            {
                gameObject.SetActive(false);
                m_SkeletonPool.Enqueue(gameObject);
            }
            m_SkeletonList.Clear();
        }

        private void AddToPool(Queue<GameObject> pool, NativeArray<Entity> viewEntityArray)
        {
            for (int entityIndex = 0; entityIndex < viewEntityArray.Length; entityIndex++)
            {
                AddToPool(pool, viewEntityArray[entityIndex]);
            }
        }

        private void AddToPool(Queue<GameObject> pool, Entity viewEntity)
        {
            var gameObject = EntityManager.GetComponentObject<Transform>(viewEntity).gameObject;
            gameObject.SetActive(false);
            pool.Enqueue(gameObject);
        }

        private void ApplyToPool(List<GameObject> gameObjectList, Entity entity, Entity viewEntity)
        {
            var gameObject = EntityManager.GetComponentObject<Transform>(viewEntity).gameObject;
            gameObjectList.Add(gameObject);

            PostUpdateCommands.RemoveComponent<ViewReference>(entity);
        }
    }
}