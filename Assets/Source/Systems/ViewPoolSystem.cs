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
        private ComponentGroup m_ViewGroup;
        private ComponentGroup m_InvisibleGroup;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_KnightPool = new Queue<GameObject>();
            m_OrcWolfRiderPool = new Queue<GameObject>();
            m_SkeletonPool = new Queue<GameObject>();
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
                        ApplyToPool(entityArray[entityIndex], viewReferenceArray[entityIndex].Value);
                    }
                }
                else if (chunk.Has(orcWolfRiderType))
                {
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        ApplyToPool(entityArray[entityIndex], viewReferenceArray[entityIndex].Value);
                    }
                }
                if (chunk.Has(skeletonType))
                {
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        ApplyToPool(entityArray[entityIndex], viewReferenceArray[entityIndex].Value);
                    }
                }
            }

            invisibleChunkArray.Dispose();
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
            var meshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.enabled = false;
            }

            gameObject.GetComponentInChildren<Animator>().enabled = false;
            gameObject.SetActive(false);
            pool.Enqueue(gameObject);
        }

        private void ApplyToPool(Entity entity, Entity viewEntity)
        {
            PostUpdateCommands.RemoveComponent<LocalToParent>(viewEntity);
            PostUpdateCommands.RemoveComponent<Parent>(viewEntity);

            PostUpdateCommands.RemoveComponent<ViewReference>(entity);
        }
    }
}