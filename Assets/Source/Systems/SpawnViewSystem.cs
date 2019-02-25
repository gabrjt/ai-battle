using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Systems
{
    public class SpawnViewSystem : ComponentSystem
    {
        private enum ViewType
        {
            Knight,
            OrcWolfRider
        }

        private struct SpawnData
        {
            public Entity Owner;

            public ViewType ViewType;
        }

        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        private GameObject m_OrvWolfRiderPrefab;

        private GameObject m_KnightPrefab;

        private NativeList<SpawnData> m_SpawnList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                Any = new[] { ComponentType.ReadOnly<Knight>(), ComponentType.ReadOnly<OrcWolfRider>() },
                None = new[] { ComponentType.ReadOnly<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Character>() }
            });

            Debug.Assert(m_KnightPrefab = Resources.Load<GameObject>("Knight"));
            Debug.Assert(m_OrvWolfRiderPrefab = Resources.Load<GameObject>("Orc Wolf Rider"));

            m_SpawnList = new NativeList<SpawnData>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var initializedType = GetArchetypeChunkComponentType<Initialized>(true);
            var knightType = GetArchetypeChunkComponentType<Knight>(true);
            var orcWolfRiderType = GetArchetypeChunkComponentType<OrcWolfRider>(true);
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
                            m_SpawnList.Add(new SpawnData { Owner = entity, ViewType = ViewType.Knight });
                        }
                        else if (chunk.Has(orcWolfRiderType))
                        {
                            m_SpawnList.Add(new SpawnData { Owner = entity, ViewType = ViewType.OrcWolfRider });
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
                GameObject view;

                switch (m_SpawnList[i].ViewType)
                {
                    case ViewType.Knight:
                        view = Object.Instantiate(m_KnightPrefab);
                        break;

                    case ViewType.OrcWolfRider:
                        view = Object.Instantiate(m_OrvWolfRiderPrefab);
                        break;

                    default:
                        continue;
                }

                var entity = view.GetComponent<GameObjectEntity>().Entity;
                var owner = m_SpawnList[i].Owner;

                EntityManager.SetComponentData(entity, new View
                {
                    Owner = owner,
                    Offset = new float3(0, -1, 0)
                });

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