using Game.Components;
using Game.Extensions;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
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
                None = new[] { ComponentType.ReadWrite<Destination>(), ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Dying>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Target>() },
                None = new[] { ComponentType.ReadWrite<Destination>() }
            });
        }

        protected override void OnUpdate()
        {
            var terrain = Terrain.activeTerrain;
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var targetType = GetArchetypeChunkComponentType<Target>(true);
            var translationFromEntity = GetComponentDataFromEntity<Translation>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                if (chunk.Has(targetType))
                {
                    var targetArray = chunk.GetNativeArray(targetType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.AddComponent(entityArray[entityIndex], new Destination { Value = translationFromEntity[targetArray[entityIndex].Value].Value });
                    }
                }
                else
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.AddComponent(entityArray[entityIndex], new Destination { Value = terrain.GetRandomPosition() });
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}