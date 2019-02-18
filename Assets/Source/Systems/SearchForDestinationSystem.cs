using Game.Components;
using Game.Extensions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Systems
{
    public class SearchForDestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        [Inject]
        private SpawnAICharactersSystem m_SpawnAICharactersSystem;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<SearchingForDestination>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>() }
            });
        }

        protected override void OnUpdate()
        {
            var terrain = Terrain.activeTerrain;

            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    if (NavMesh.SamplePosition(terrain.GetRandomPosition(), out var hit, 1, NavMesh.AllAreas))
                    {
                        PostUpdateCommands.AddComponent(entity, new DestinationFound { Value = hit.position });
                        PostUpdateCommands.RemoveComponent<SearchingForDestination>(entity);
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}