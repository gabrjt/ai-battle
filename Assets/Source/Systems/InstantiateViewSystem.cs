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
        private struct ProcessJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<Entity> EntityArray;
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

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            Debug.Assert(m_KnightPrefab = Resources.Load<GameObject>("Knight"));
            Debug.Assert(m_OrcWolfRiderPrefab = Resources.Load<GameObject>("Orc Wolf Rider"));
            Debug.Assert(m_SkeletonPrefab = Resources.Load<GameObject>("Skeleton"));
            m_KnightGroup = Entities.WithAll<ViewInfo, ViewVisible, Translation, Rotation, Knight>().WithNone<ViewReference>().ToComponentGroup();
            m_OrcWolfRiderGroup = Entities.WithAll<ViewInfo, ViewVisible, Translation, Rotation, OrcWolfRider>().WithNone<ViewReference>().ToComponentGroup();
            m_SkeletonGroup = Entities.WithAll<ViewInfo, ViewVisible, Translation, Rotation, Skeleton>().WithNone<ViewReference>().ToComponentGroup();
        }

        protected override void OnUpdate()
        {
            var viewPoolSystem = World.GetExistingManager<ViewPoolSystem>();
            InstatianteViews(viewPoolSystem.m_KnightPool, ViewType.Knight, m_KnightPrefab, m_KnightGroup.CalculateLength(), m_KnightGroup.ToEntityArray(Allocator.TempJob));
            InstatianteViews(viewPoolSystem.m_OrcWolfRiderPool, ViewType.OrcWolfRider, m_OrcWolfRiderPrefab, m_OrcWolfRiderGroup.CalculateLength(), m_OrcWolfRiderGroup.ToEntityArray(Allocator.TempJob));
            InstatianteViews(viewPoolSystem.m_SkeletonPool, ViewType.Skeleton, m_SkeletonPrefab, m_SkeletonGroup.CalculateLength(), m_SkeletonGroup.ToEntityArray(Allocator.TempJob));
        }

        private void InstatianteViews(Queue<GameObject> pool, ViewType type, GameObject prefab, int length, NativeArray<Entity> entityArray)
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

            entityArray.Dispose();
            translationArray.Dispose();
            rotationArray.Dispose();
        }
    }
}