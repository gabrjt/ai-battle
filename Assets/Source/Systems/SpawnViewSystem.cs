using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class SpawnViewSystem : ComponentSystem
    {
        private enum ViewType
        {
            Knight,
            OrcWolfRider,
            Skeleton
        }

        private struct SpawnData
        {
            public Entity Owner;

            public ViewType ViewType;
        }

        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        private GameObject m_KnightPrefab;

        private GameObject m_OrvWolfRiderPrefab;

        private GameObject m_SkeletonPrefab;

        private NativeList<SpawnData> m_SpawnList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<Rotation>() },
                Any = new[] { ComponentType.ReadOnly<Knight>(), ComponentType.ReadOnly<OrcWolfRider>(), ComponentType.ReadOnly<Skeleton>() },
                None = new[] { ComponentType.ReadOnly<Initialized>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<Rotation>() }
            });

            Debug.Assert(m_KnightPrefab = Resources.Load<GameObject>("Knight"));
            Debug.Assert(m_OrvWolfRiderPrefab = Resources.Load<GameObject>("Orc Wolf Rider"));
            Debug.Assert(m_SkeletonPrefab = Resources.Load<GameObject>("Skeleton"));

            m_SpawnList = new NativeList<SpawnData>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var initializedType = GetArchetypeChunkComponentType<Initialized>(true);
            var knightType = GetArchetypeChunkComponentType<Knight>(true);
            var orcWolfRiderType = GetArchetypeChunkComponentType<OrcWolfRider>(true);
            var skeletonType = GetArchetypeChunkComponentType<Skeleton>(true);

            for (int chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                if (!chunk.Has(initializedType))
                {
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        PostUpdateCommands.AddComponent(entity, new Initialized());

                        if (chunk.Has(knightType))
                        {
                            m_SpawnList.Add(new SpawnData
                            {
                                Owner = entity,
                                ViewType = ViewType.Knight,
                            });
                        }
                        else if (chunk.Has(orcWolfRiderType))
                        {
                            m_SpawnList.Add(new SpawnData
                            {
                                Owner = entity,
                                ViewType = ViewType.OrcWolfRider,
                            });
                        }
                        else if (chunk.Has(skeletonType))
                        {
                            m_SpawnList.Add(new SpawnData
                            {
                                Owner = entity,
                                ViewType = ViewType.Skeleton,
                            });
                        }
                    }
                }
                else
                {
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        PostUpdateCommands.RemoveComponent<Initialized>(entity);
                    }
                }
            }

            chunkArray.Dispose();

            for (int i = 0; i < m_SpawnList.Length; i++)
            {
                var spawnData = m_SpawnList[i];

                var owner = spawnData.Owner;

                var position = EntityManager.GetComponentData<Position>(owner).Value;
                var rotation = EntityManager.GetComponentData<Rotation>(owner).Value;

                GameObject view;

                switch (spawnData.ViewType)
                {
                    case ViewType.Knight:
                        view = Object.Instantiate(m_KnightPrefab, position, rotation);
                        break;

                    case ViewType.OrcWolfRider:
                        view = Object.Instantiate(m_OrvWolfRiderPrefab, position, rotation);
                        break;

                    case ViewType.Skeleton:
                        view = Object.Instantiate(m_SkeletonPrefab, position, rotation);
                        break;

                    default:
                        continue;
                }

                var entity = view.GetComponent<GameObjectEntity>().Entity;

                EntityManager.SetComponentData(entity, new View
                {
                    Owner = owner,
                    Offset = new float3(0, -1, 0)
                });
                EntityManager.SetComponentData(entity, new Position { Value = position });
                EntityManager.SetComponentData(entity, new Rotation { Value = rotation });

                EntityManager.AddComponentData(owner, new ViewReference { Value = entity });
            }

            m_SpawnList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SpawnList.IsCreated)
            {
                m_SpawnList.Dispose();
            }
        }
    }
}