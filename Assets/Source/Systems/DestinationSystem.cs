using Game.Components;
using Game.Extensions;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(GameLogicGroup))]
    [UpdateAfter(typeof(IdleSystem))]
    public class DestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<Destination>(), ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dying>() }
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

                    PostUpdateCommands.AddComponent(entity, new Destination { Value = terrain.GetRandomPosition() });
                }
            }

            chunkArray.Dispose();
        }
    }
}