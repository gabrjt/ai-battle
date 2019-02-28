using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    public class SetDeadSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_SetDeadList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Health>() },
                None = new[] { ComponentType.Create<Dead>() }
            },
            new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<Killed>() }
            });

            m_SetDeadList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var healthType = GetArchetypeChunkComponentType<Health>(true);
            var killedType = GetArchetypeChunkComponentType<Killed>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(healthType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);
                    var healthArray = chunk.GetNativeArray(healthType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var health = healthArray[entityIndex];

                        if (health.Value > 0 || m_SetDeadList.Contains(entity)) continue;

                        m_SetDeadList.Add(entity);

                        PostUpdateCommands.AddComponent(entity, new Dead
                        {
                            Duration = 5,
                            StartTime = Time.time
                        });
                    }
                }
                else if (chunk.Has(killedType))
                {
                    var killedArray = chunk.GetNativeArray(killedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = killedArray[entityIndex].Other;

                        if (EntityManager.HasComponent<Dead>(entity) || m_SetDeadList.Contains(entity)) continue;

                        m_SetDeadList.Add(entity);

                        PostUpdateCommands.AddComponent(entity, new Dead
                        {
                            Duration = 5,
                            StartTime = Time.time
                        });
                    }
                }
            }

            chunkArray.Dispose();

            m_SetDeadList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SetDeadList.IsCreated)
            {
                m_SetDeadList.Dispose();
            }
        }
    }
}