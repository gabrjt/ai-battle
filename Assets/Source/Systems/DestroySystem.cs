using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [UpdateAfter(typeof(EndFrameBarrier))]
    public class DestroySystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>() }
            });
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

                    if (EntityManager.HasComponent<Transform>(entity))
                    {
                        var transform = EntityManager.GetComponentObject<Transform>(entity);
                        Object.Destroy(transform.gameObject);
                    }
                    else
                    {
                        PostUpdateCommands.DestroyEntity(entity);
                    }
                }
            }

            chunkArray.Dispose();

            // EntityManager.DestroyEntity(m_Group);
        }
    }
}