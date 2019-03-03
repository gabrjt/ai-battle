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
        internal class PoolData
        {
            public bool IsValid;

            public GameObject GameObject;
        }

        private ComponentGroup m_Group;

        internal Queue<PoolData> m_CharacterPool;

        internal Queue<PoolData> m_KnightPool;

        internal Queue<PoolData> m_OrcWolfRiderPool;

        internal Queue<PoolData> m_SkeletonPool;

        internal Queue<PoolData> m_HealthBarPool;

        private List<GameObject> m_DestroyGarbageList;

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

            m_CharacterPool = new Queue<PoolData>();
            m_KnightPool = new Queue<PoolData>();
            m_OrcWolfRiderPool = new Queue<PoolData>();
            m_SkeletonPool = new Queue<PoolData>();
            m_HealthBarPool = new Queue<PoolData>();
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
                        var entity = entityArray[entityIndex];
                        var gameObject = EntityManager.GetComponentObject<NavMeshAgent>(entity).gameObject;

                        m_CharacterPool.Enqueue(new PoolData
                        {
                            IsValid = true,
                            GameObject = gameObject
                        });
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
                                m_KnightPool.Enqueue(new PoolData
                                {
                                    IsValid = true,
                                    GameObject = gameObject
                                });
                                break;

                            case ViewType.OrcWolfRider:
                                m_OrcWolfRiderPool.Enqueue(new PoolData
                                {
                                    IsValid = true,
                                    GameObject = gameObject
                                });
                                break;

                            case ViewType.Skeleton:
                                m_SkeletonPool.Enqueue(new PoolData
                                {
                                    IsValid = true,
                                    GameObject = gameObject
                                });
                                break;
                        }
                    }
                }
                else if (chunk.Has(healthBarType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var gameObject = EntityManager.GetComponentObject<RectTransform>(entity).parent.gameObject;

                        m_HealthBarPool.Enqueue(new PoolData
                        {
                            IsValid = true,
                            GameObject = gameObject
                        });
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

            foreach (var poolData in m_CharacterPool)
            {
                ApplyToPool(poolData);
            }

            foreach (var poolData in m_KnightPool)
            {
                ApplyToPool(poolData);
            }

            foreach (var poolData in m_OrcWolfRiderPool)
            {
                ApplyToPool(poolData);
            }

            foreach (var poolData in m_SkeletonPool)
            {
                ApplyToPool(poolData);
            }

            foreach (var poolData in m_HealthBarPool)
            {
                ApplyToPool(poolData);
            }

            base.OnUpdate();
        }

        private void ApplyToPool(PoolData poolData)
        {
            var gameObject = poolData.GameObject;

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