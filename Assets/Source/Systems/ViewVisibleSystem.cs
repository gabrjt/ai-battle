using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class ViewVisibleSystem : ComponentSystem
    {
        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<View>(), ComponentType.ReadOnly<Visible>() },
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
                        SetEnabledSkinnedMeshRenderers(entity, true);
                    }
                }
                else if (chunk.Has(initializedType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        PostUpdateCommands.RemoveComponent<Initialized>(entity);
                        SetEnabledSkinnedMeshRenderers(entity, false);
                    }
                }
            }

            chunkArray.Dispose();
        }

        private void SetEnabledSkinnedMeshRenderers(Entity entity, bool enabled)
        {
            if (!EntityManager.HasComponent<Transform>(entity)) return;

            var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;

            var meshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.enabled = enabled;
            }
        }
    }
}