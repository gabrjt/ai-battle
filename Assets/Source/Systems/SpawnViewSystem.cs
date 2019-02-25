using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Systems
{
    public class SpawnViewSystem : ComponentSystem
    {
        private struct SpawnData
        {
            public Entity Owner;
        }

        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        private GameObject m_Prefab;

        private NativeList<SpawnData> m_SpawnList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadOnly<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Character>() }
            });

            Debug.Assert(m_Prefab = Resources.Load<GameObject>("Orc Wolf Rider"));

            m_SpawnList = new NativeList<SpawnData>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var initializedType = GetArchetypeChunkComponentType<Initialized>(true);
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
                        m_SpawnList.Add(new SpawnData { Owner = entity });
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
                var view = Object.Instantiate(m_Prefab);
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