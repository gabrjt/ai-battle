using Game.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class DestroyHealthBarSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private List<GameObject> m_GameObjectList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadOnly<Destroy>() }
            });

            m_GameObjectList = new List<GameObject>();
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    if (EntityManager.HasComponent<RectTransform>(entity))
                    {
                        m_GameObjectList.Add(EntityManager.GetComponentObject<RectTransform>(entity).transform.parent.gameObject);
                    }
                    else
                    {
                        PostUpdateCommands.DestroyEntity(entity);
                    }
                }
            }

            chunkArray.Dispose();

            foreach (var gameObject in m_GameObjectList)
            {
                Object.Destroy(gameObject);
            }

            m_GameObjectList.Clear();
        }
    }
}