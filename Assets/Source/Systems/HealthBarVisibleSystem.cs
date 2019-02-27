using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Systems
{
    public class HealthBarVisibleSystem : ComponentSystem
    {
        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadOnly<Visible>() },
                None = new[] { ComponentType.Create<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.Create<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Visible>() }
            });

            RequireSingletonForUpdate<CameraSingleton>();
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var initializedType = GetArchetypeChunkComponentType<Initialized>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                if (!chunk.Has(initializedType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        PostUpdateCommands.AddComponent(entity, new Initialized());
                        SetEnabledImages(entity, true);
                    }
                }
                else if (chunk.Has(initializedType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        PostUpdateCommands.RemoveComponent<Initialized>(entity);
                        SetEnabledImages(entity, false);
                    }
                }
            }

            chunkArray.Dispose();
        }

        private void SetEnabledImages(Entity entity, bool enabled)
        {
            if (!EntityManager.HasComponent<RectTransform>(entity)) return;

            var gameObject = EntityManager.GetComponentObject<RectTransform>(entity).parent.gameObject;

            var images = gameObject.GetComponentsInChildren<Image>();
            foreach (var image in images)
            {
                image.enabled = enabled;
            }
        }
    }
}