using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class SpawnHealthBarsSystem : ComponentSystem
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
                All = new[] { ComponentType.ReadOnly<Health>(), ComponentType.ReadOnly<MaximumHealth>() },
                None = new[] { ComponentType.ReadOnly<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Health>(), ComponentType.ReadOnly<MaximumHealth>() }
            });

            m_Prefab = Resources.Load<GameObject>("Health Bar");

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

            var canvas = GetSingleton<CanvasSingleton>();
            var transform = EntityManager.GetComponentObject<RectTransform>(canvas.Owner);

            for (int i = 0; i < m_SpawnList.Length; i++)
            {
                var healthBar = Object.Instantiate(m_Prefab, transform);

                EntityManager.SetComponentData(healthBar.GetComponentInChildren<GameObjectEntity>().Entity, new HealthBar
                {
                    Owner = m_SpawnList[i].Owner,
                    Visible = true
                });
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