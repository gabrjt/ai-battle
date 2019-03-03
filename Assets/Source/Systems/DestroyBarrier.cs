using Game.Components;
using Game.Enums;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Systems
{
    [AlwaysUpdateSystem]
    [UpdateAfter(typeof(EventBarrier))]
    public class DestroyBarrier : BarrierSystem
    {
        private ComponentGroup m_Group;

        internal Queue<GameObject> m_CharacterPool;

        internal Queue<GameObject> m_KnightPool;

        internal Queue<GameObject> m_OrcWolfRiderPool;

        internal Queue<GameObject> m_SkeletonPool;

        internal Queue<GameObject> m_HealthBarPool;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>() },
                None = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadOnly<View>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<HealthBar>() },
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<View>() },
            });

            m_CharacterPool = new Queue<GameObject>();
            m_KnightPool = new Queue<GameObject>();
            m_OrcWolfRiderPool = new Queue<GameObject>();
            m_SkeletonPool = new Queue<GameObject>();
            m_HealthBarPool = new Queue<GameObject>();
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var characterType = GetArchetypeChunkComponentType<Character>(true);
            var viewType = GetArchetypeChunkComponentType<View>(true);
            var healthBarType = GetArchetypeChunkComponentType<HealthBar>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                if (chunk.Has(characterType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        m_CharacterPool.Enqueue(EntityManager.GetComponentObject<NavMeshAgent>(entityArray[entityIndex]).gameObject);
                    }
                }
                else if (chunk.Has(viewType))
                {
                    var viewArray = chunk.GetNativeArray(viewType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var view = viewArray[entityIndex];
                        var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;

                        switch (view.Value)
                        {
                            case ViewType.Knight:
                                m_KnightPool.Enqueue(gameObject);
                                break;

                            case ViewType.OrcWolfRider:
                                m_OrcWolfRiderPool.Enqueue(gameObject);
                                break;

                            case ViewType.Skeleton:
                                m_SkeletonPool.Enqueue(gameObject);
                                break;
                        }
                    }
                }
                else if (chunk.Has(healthBarType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        m_HealthBarPool.Enqueue(EntityManager.GetComponentObject<RectTransform>(entityArray[entityIndex]).parent.gameObject);
                    }
                }
                else
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.DestroyEntity(entityArray[entityIndex]); // should be in Apply, but since this is a classic Chunk Iteration...
                    }
                }
            }

            chunkArray.Dispose();

            foreach (var gameObject in m_CharacterPool)
            {
                ApplyToPool(gameObject);
            }

            foreach (var gameObject in m_KnightPool)
            {
                ApplyToPool(gameObject);
            }

            foreach (var gameObject in m_OrcWolfRiderPool)
            {
                ApplyToPool(gameObject);
            }

            foreach (var gameObject in m_SkeletonPool)
            {
                ApplyToPool(gameObject);
            }

            foreach (var gameObject in m_HealthBarPool)
            {
                ApplyToPool(gameObject);
            }

            var spawnAICharacterSystem = World.GetExistingManager<SpawnAICharacterSystem>();

            var maxPoolCount = spawnAICharacterSystem.m_TotalCount + spawnAICharacterSystem.m_TotalCount / 2;

            while (m_CharacterPool.Count > maxPoolCount)
            {
                Object.Destroy(m_CharacterPool.Dequeue());
            }

            while (m_KnightPool.Count > maxPoolCount)
            {
                Object.Destroy(m_KnightPool.Dequeue());
            }

            while (m_OrcWolfRiderPool.Count > maxPoolCount)
            {
                Object.Destroy(m_OrcWolfRiderPool.Dequeue());
            }

            while (m_SkeletonPool.Count > maxPoolCount)
            {
                Object.Destroy(m_SkeletonPool.Dequeue());
            }

            while (m_HealthBarPool.Count > maxPoolCount)
            {
                Object.Destroy(m_HealthBarPool.Dequeue());
            }

            base.OnUpdate();
        }

        private void ApplyToPool(GameObject gameObject)
        {
            if (gameObject.activeInHierarchy)
            {
                gameObject.SetActive(false);
            }
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            m_CharacterPool.Clear();
            m_KnightPool.Clear();
            m_OrcWolfRiderPool.Clear();
            m_SkeletonPool.Clear();
            m_HealthBarPool.Clear();
        }
    }
}