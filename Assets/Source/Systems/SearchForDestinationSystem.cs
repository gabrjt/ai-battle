using Game.Components;
using Game.Extensions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
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

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<DestinationFound>());
        }

        protected override void OnUpdate()
        {
            var eventCommandBuffer = World.GetExistingManager<EventCommandBufferSystem>().CreateCommandBuffer();
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
                    var destinationFound = eventCommandBuffer.CreateEntity(m_Archetype);

                    eventCommandBuffer.SetComponent(destinationFound, new DestinationFound
                    {
                        This = entity,
                        Value = terrain.GetRandomPosition()
                    });
                }
            }

            chunkArray.Dispose();
        }
    }
}