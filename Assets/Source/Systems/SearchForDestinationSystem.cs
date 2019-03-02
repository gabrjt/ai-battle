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

        private EntityArchetype m_Archetype;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<SearchingForDestination>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<DestinationFound>());
        }

        protected override void OnUpdate()
        {
            var commandBuffer = World.GetExistingManager<EventBarrier>().CreateCommandBuffer();

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
                        var destinationFound = commandBuffer.CreateEntity(m_Archetype);
                        commandBuffer.SetComponent(destinationFound, new DestinationFound
                        {
                            This = entity,
                            Value = hit.position
                        });
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}