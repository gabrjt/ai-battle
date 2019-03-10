using Game.Comparers;
using Game.Components;
using Game.Enums;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(EntityLifecycleGroup))]
    [UpdateAfter(typeof(InstantiateAICharacterSystem))]
    public class InstantiateViewSystem : ComponentSystem
    {
        [BurstCompile]
        private struct SortJob : IJob
        {
            public NativeArray<Entity> KnightArray;
            public NativeArray<Entity> OrcWolfRiderArray;
            public NativeArray<Entity> SkeletonArray;
            [ReadOnly] public EntityDistanceFromTranslationComparer Comparer;

            public void Execute()
            {
                KnightArray.Sort(Comparer);
                OrcWolfRiderArray.Sort(Comparer);
                SkeletonArray.Sort(Comparer);
            }
        }

        private GameObject m_KnightPrefab;
        private GameObject m_OrcWolfRiderPrefab;
        private GameObject m_SkeletonPrefab;
        private ComponentGroup m_KnightGroup;
        private ComponentGroup m_OrcWolfRiderGroup;
        private ComponentGroup m_SkeletonGroup;
        private ComponentGroup m_VisibleGroup;
        private ComponentGroup m_CameraGroup;
        private EntityDistanceFromTranslationComparer m_Comparer;
        internal int m_MaxViewCount = 300;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            Debug.Assert(m_KnightPrefab = Resources.Load<GameObject>("Knight"));
            Debug.Assert(m_OrcWolfRiderPrefab = Resources.Load<GameObject>("Orc Wolf Rider"));
            Debug.Assert(m_SkeletonPrefab = Resources.Load<GameObject>("Skeleton"));
            m_KnightGroup = Entities.WithAll<ViewInfo, ViewVisible, Translation, Rotation, Knight>().WithNone<ViewReference>().ToComponentGroup();
            m_OrcWolfRiderGroup = Entities.WithAll<ViewInfo, ViewVisible, Translation, Rotation, OrcWolfRider>().WithNone<ViewReference>().ToComponentGroup();
            m_SkeletonGroup = Entities.WithAll<ViewInfo, ViewVisible, Translation, Rotation, Skeleton>().WithNone<ViewReference>().ToComponentGroup();
            m_VisibleGroup = Entities.WithAll<ViewReference, ViewVisible>().ToComponentGroup();
            m_CameraGroup = Entities.WithAll<Components.CameraArm, Translation>().ToComponentGroup();
            m_Comparer = new EntityDistanceFromTranslationComparer();

            RequireSingletonForUpdate<Components.CameraArm>();
        }

        protected override void OnUpdate()
        {
            var visibleGroupLength = m_VisibleGroup.CalculateLength();

            if (visibleGroupLength > m_MaxViewCount) return;

            var cameraTranslationArray = m_CameraGroup.ToComponentDataArray<Translation>(Allocator.TempJob);
            m_Comparer.Translation = cameraTranslationArray[0].Value;
            cameraTranslationArray.Dispose();

            var viewPoolSystem = World.GetExistingManager<ViewPoolSystem>();

            var knightGroupLength = m_KnightGroup.CalculateLength();
            var knightArray = m_KnightGroup.ToEntityArray(Allocator.TempJob);
            var orcWolfRiderGroupLength = m_OrcWolfRiderGroup.CalculateLength();
            var orcWolfRiderArray = m_OrcWolfRiderGroup.ToEntityArray(Allocator.TempJob);
            var skeletonGroupLength = m_SkeletonGroup.CalculateLength();
            var skeletonArray = m_SkeletonGroup.ToEntityArray(Allocator.TempJob);
            m_Comparer.TranslationFromEntity = GetComponentDataFromEntity<Translation>(true);

            new SortJob
            {
                KnightArray = knightArray,
                OrcWolfRiderArray = orcWolfRiderArray,
                SkeletonArray = skeletonArray,
                Comparer = m_Comparer
            }.Schedule().Complete();

            var hasKnight = knightGroupLength > 0;
            var hasOrcWolfRider = orcWolfRiderGroupLength > 0;
            var hasSkeleton = skeletonGroupLength > 0;
            var knightIndex = 0;
            var orcWolfRiderIndex = 0;
            var skeletonIndex = 0;
            var lastCount = visibleGroupLength;
            var totalGroupLength = knightGroupLength + orcWolfRiderGroupLength + skeletonGroupLength;
            do
            {
                lastCount = visibleGroupLength;

                if (hasKnight && knightIndex < knightGroupLength && visibleGroupLength < m_MaxViewCount)
                {
                    var entity = knightArray[knightIndex];
                    Instantiate(viewPoolSystem.m_KnightPool, ViewType.Knight, m_KnightPrefab, entity, EntityManager.GetComponentData<Translation>(entity), EntityManager.GetComponentData<Rotation>(entity));
                    ++knightIndex;
                    ++visibleGroupLength;
                }

                if (hasOrcWolfRider && orcWolfRiderIndex < orcWolfRiderGroupLength && visibleGroupLength < m_MaxViewCount)
                {
                    var entity = orcWolfRiderArray[orcWolfRiderIndex];
                    Instantiate(viewPoolSystem.m_OrcWolfRiderPool, ViewType.OrcWolfRider, m_OrcWolfRiderPrefab, entity, EntityManager.GetComponentData<Translation>(entity), EntityManager.GetComponentData<Rotation>(entity));
                    ++orcWolfRiderIndex;
                    ++visibleGroupLength;
                }

                if (hasSkeleton && skeletonIndex < skeletonGroupLength && visibleGroupLength < m_MaxViewCount)
                {
                    var entity = skeletonArray[skeletonIndex];
                    Instantiate(viewPoolSystem.m_SkeletonPool, ViewType.Skeleton, m_SkeletonPrefab, entity, EntityManager.GetComponentData<Translation>(entity), EntityManager.GetComponentData<Rotation>(entity));
                    ++skeletonIndex;
                    ++visibleGroupLength;
                }
            } while (lastCount != visibleGroupLength && visibleGroupLength < m_MaxViewCount && knightIndex + orcWolfRiderIndex + skeletonIndex < totalGroupLength);

            knightArray.Dispose();
            orcWolfRiderArray.Dispose();
            skeletonArray.Dispose();
        }

        private void Instantiate(Queue<GameObject> pool, ViewType type, GameObject prefab, Entity entity, Translation translation, Rotation rotation)
        {
            var gameObject = pool.Count > 0 ? pool.Dequeue() : Object.Instantiate(prefab);
            gameObject.transform.position = translation.Value;
            gameObject.transform.rotation = rotation.Value;
            var meshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.enabled = true;
            }

            gameObject.GetComponent<CopyTransformToGameObjectProxy>().enabled = true;
            gameObject.GetComponentInChildren<Animator>().enabled = true;
            gameObject.SetActive(true);
            var viewEntity = gameObject.GetComponent<GameObjectEntity>().Entity;

            EntityManager.AddComponentData(entity, new ViewReference { Value = viewEntity });

            EntityManager.AddComponentData(viewEntity, new Parent { Value = entity });
            EntityManager.AddComponentData(viewEntity, new LocalToParent());

            var name = $"{type} View {viewEntity}";
#if UNITY_EDITOR
            EntityManager.SetName(viewEntity, name);
#endif
            gameObject.name = name;
        }
    }
}