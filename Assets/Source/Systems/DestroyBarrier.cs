using Game.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [AlwaysUpdateSystem]
    [UpdateAfter(typeof(EventBarrier))]
    public class DestroyBarrier : BarrierSystem
    {
        private ComponentGroup m_Group;

        private List<GameObject> m_GameObjectList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>() },
                None = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadOnly<View>() } // TODO: transform, rectTransform components;
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<HealthBar>() },
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>(), ComponentType.ReadOnly<View>() },
            });

            m_GameObjectList = new List<GameObject>();
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var rectTransformType = GetArchetypeChunkComponentType<HealthBar>(true);
            var transformType = GetArchetypeChunkComponentType<View>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                if (chunk.Has(rectTransformType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        m_GameObjectList.Add(EntityManager.GetComponentObject<RectTransform>(entity).parent.gameObject);
                    }
                }
                else if (chunk.Has(transformType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        m_GameObjectList.Add(EntityManager.GetComponentObject<Transform>(entity).gameObject);
                    }
                }
                else
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
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

            base.OnUpdate();
        }
    }
}