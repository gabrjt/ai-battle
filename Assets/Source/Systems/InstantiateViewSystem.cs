using Game.Comparers;
using Game.Components;
using Game.Enums;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
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
            public EntityDistanceComparer Comparer;
            public NativeArray<Entity> EntityArray;

            public void Execute()
            {
                EntityArray.Sort(Comparer);
            }
        }

        [BurstCompile]
        private struct ProcessJob : IJobParallelFor
        {
            [ReadOnly] public NativeSlice<Entity> EntityArray;
            [NativeDisableParallelForRestriction] public NativeArray<Translation> TranslationArray;
            [NativeDisableParallelForRestriction] public NativeArray<Rotation> RotationArray;
            [ReadOnly] public ComponentDataFromEntity<Translation> TranslationFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Rotation> RotationFromEntity;

            public void Execute(int index)
            {
                var entity = EntityArray[index];
                TranslationArray[index] = TranslationFromEntity[entity];
                RotationArray[index] = RotationFromEntity[entity];
            }
        }

        private GameObject m_KnightPrefab;
        private GameObject m_OrcWolfRiderPrefab;
        private GameObject m_SkeletonPrefab;
        private ComponentGroup m_KnightGroup;
        private ComponentGroup m_OrcWolfRiderGroup;
        private ComponentGroup m_SkeletonGroup;
        private ComponentGroup m_VisibleGroup;
        private EntityDistanceComparer m_Comparer;
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
            m_Comparer = new EntityDistanceComparer();

            RequireSingletonForUpdate<CameraArm>();
        }

        protected override void OnUpdate()
        {
            var visibleGroupLength = m_VisibleGroup.CalculateLength();

            if (visibleGroupLength > m_MaxViewCount) return;

            var cameraTranslation = float3.zero;
            Entities.WithAll<CameraArm, Translation>().ForEach((ref Translation translation) =>
            {
                cameraTranslation = translation.Value;
            });
            m_Comparer.Translation = cameraTranslation;

            var viewPoolSystem = World.GetExistingManager<ViewPoolSystem>();
            var maxViewTypeCount = (m_MaxViewCount - visibleGroupLength) / 3;

            var knightGroupLength = m_KnightGroup.CalculateLength();
            if (knightGroupLength > 0)
            {
                Instantiate(viewPoolSystem.m_KnightPool, ViewType.Knight, m_KnightPrefab, m_KnightGroup, maxViewTypeCount, knightGroupLength);
            }

            var orcWolfRiderGroupLength = m_OrcWolfRiderGroup.CalculateLength();
            if (orcWolfRiderGroupLength > 0)
            {
                Instantiate(viewPoolSystem.m_OrcWolfRiderPool, ViewType.OrcWolfRider, m_OrcWolfRiderPrefab, m_OrcWolfRiderGroup, maxViewTypeCount, orcWolfRiderGroupLength);
            }

            var skeletonGroupLength = m_SkeletonGroup.CalculateLength();
            if (skeletonGroupLength > 0)
            {
                Instantiate(viewPoolSystem.m_SkeletonPool, ViewType.Skeleton, m_SkeletonPrefab, m_SkeletonGroup, maxViewTypeCount, skeletonGroupLength);
            }
        }

        private void Instantiate(Queue<GameObject> pool, ViewType type, GameObject prefab, ComponentGroup group, int maxViewTypeCount, int groupLength)
        {
            var length = math.select(groupLength, maxViewTypeCount, groupLength > maxViewTypeCount);
            var entityArray = group.ToEntityArray(Allocator.TempJob);
            m_Comparer.TranslationFromEntity = GetComponentDataFromEntity<Translation>(true);

            new SortJob
            {
                EntityArray = entityArray,
                Comparer = m_Comparer
            }.Schedule().Complete();

            InstatianteViews(pool, type, prefab, length, new NativeSlice<Entity>(entityArray, 0, length));
            entityArray.Dispose();
        }

        private void InstatianteViews(Queue<GameObject> pool, ViewType type, GameObject prefab, int length, NativeSlice<Entity> entityArray)
        {
            var translationArray = new NativeArray<Translation>(length, Allocator.TempJob);
            var rotationArray = new NativeArray<Rotation>(length, Allocator.TempJob);

            new ProcessJob
            {
                EntityArray = entityArray,
                TranslationArray = translationArray,
                RotationArray = rotationArray,
                TranslationFromEntity = GetComponentDataFromEntity<Translation>(true),
                RotationFromEntity = GetComponentDataFromEntity<Rotation>(true),
            }.Schedule(length, 64).Complete();

            for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                var entity = entityArray[entityIndex];
                var translation = translationArray[entityIndex].Value;
                var rotation = rotationArray[entityIndex].Value;
                var gameObject = pool.Count > 0 ? pool.Dequeue() : Object.Instantiate(prefab, translation, rotation);
                gameObject.transform.position = translation;
                gameObject.transform.rotation = rotation;
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

            translationArray.Dispose();
            rotationArray.Dispose();
        }
    }
}