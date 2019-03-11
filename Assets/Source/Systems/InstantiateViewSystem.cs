using Game.Components;
using Game.Enums;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameObjectPoolGroup))]
    [UpdateAfter(typeof(ViewPoolSystem))]
    public class InstantiateViewSystem : ComponentSystem
    {
        private GameObject m_KnightPrefab;
        private GameObject m_OrcWolfRiderPrefab;
        private GameObject m_SkeletonPrefab;
        private ComponentGroup m_Group;
        private NativeList<Entity> m_KnightList;
        private NativeList<Entity> m_OrcWolfRiderList;
        private NativeList<Entity> m_SkeletonList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            Debug.Assert(m_KnightPrefab = Resources.Load<GameObject>("Knight"));
            Debug.Assert(m_OrcWolfRiderPrefab = Resources.Load<GameObject>("Orc Wolf Rider"));
            Debug.Assert(m_SkeletonPrefab = Resources.Load<GameObject>("Skeleton"));

            m_Group = Entities.WithAll<Character, ViewInfo, Translation, Rotation>().WithAny<Knight, OrcWolfRider, Skeleton>().WithNone<ViewReference, Destroy, Disabled>().ToComponentGroup();
            m_KnightList = new NativeList<Entity>(Allocator.Persistent);
            m_OrcWolfRiderList = new NativeList<Entity>(Allocator.Persistent);
            m_SkeletonList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var viewPoolSystem = World.GetExistingManager<ViewPoolSystem>();
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var knightType = GetArchetypeChunkComponentType<Knight>(true);
            var orcWolfRiderType = GetArchetypeChunkComponentType<OrcWolfRider>(true);
            var skeletonType = GetArchetypeChunkComponentType<Skeleton>(true);

            for (int chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var hasOrcWolfRider = chunk.Has(orcWolfRiderType);
                var hasSkeleton = chunk.Has(skeletonType);
                var entityArray = chunk.GetNativeArray(entityType);
                {
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
            }

            chunkArray.Dispose();

            for (var entityIndex = 0; entityIndex < m_KnightList.Length; entityIndex++)
            {
                Instantiate(viewPoolSystem.m_KnightPool, ViewType.Knight, m_KnightPrefab, m_KnightList[entityIndex]);
            }

            for (var entityIndex = 0; entityIndex < m_OrcWolfRiderList.Length; entityIndex++)
            {
                Instantiate(viewPoolSystem.m_OrcWolfRiderPool, ViewType.OrcWolfRider, m_OrcWolfRiderPrefab, m_OrcWolfRiderList[entityIndex]);
            }

            for (var entityIndex = 0; entityIndex < m_SkeletonList.Length; entityIndex++)
            {
                Instantiate(viewPoolSystem.m_SkeletonPool, ViewType.Skeleton, m_SkeletonPrefab, m_SkeletonList[entityIndex]);
            }

            m_KnightList.Clear();
            m_OrcWolfRiderList.Clear();
            m_SkeletonList.Clear();
        }

        private void Instantiate(Queue<GameObject> pool, ViewType type, GameObject prefab, Entity entity)
        {
            var gameObject = pool.Count > 0 ? pool.Dequeue() : Object.Instantiate(prefab);
            gameObject.transform.position = EntityManager.GetComponentData<Translation>(entity).Value;
            gameObject.transform.rotation = EntityManager.GetComponentData<Rotation>(entity).Value;
            gameObject.SetActive(true);
            var viewEntity = gameObject.GetComponent<GameObjectEntity>().Entity;
            var name = $"{type} View {viewEntity}";
            gameObject.name = name;

            EntityManager.AddComponentData(entity, new ViewReference { Value = viewEntity });
            EntityManager.AddComponentData(viewEntity, new Parent { Value = entity });
            EntityManager.AddComponentData(viewEntity, new LocalToParent());
#if UNITY_EDITOR
            EntityManager.SetName(viewEntity, name);
#endif
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