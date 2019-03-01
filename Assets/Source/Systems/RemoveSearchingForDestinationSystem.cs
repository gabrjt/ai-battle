﻿using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(RemoveBarrier))]
    public class RemoveSearchingForDestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_RemoveSearchingForDestinationList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<SearchingForDestination>(), ComponentType.ReadOnly<Destination>() },
                Any = new[] { ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationFound>(), ComponentType.ReadOnly<Killed>() }
            });

            m_RemoveSearchingForDestinationList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var characterType = GetArchetypeChunkComponentType<Character>(true);
            var destinationFoundType = GetArchetypeChunkComponentType<DestinationFound>(true);
            var killedType = GetArchetypeChunkComponentType<Killed>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(characterType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        if (m_RemoveSearchingForDestinationList.Contains(entity)) continue;

                        m_RemoveSearchingForDestinationList.Add(entity);

                        PostUpdateCommands.RemoveComponent<SearchingForDestination>(entity);
                    }
                }
                else if (chunk.Has(destinationFoundType))
                {
                    var destinationFoundArray = chunk.GetNativeArray(destinationFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = destinationFoundArray[entityIndex].This;

                        if (!EntityManager.HasComponent<SearchingForDestination>(entity) || m_RemoveSearchingForDestinationList.Contains(entity)) continue;

                        m_RemoveSearchingForDestinationList.Add(entity);

                        PostUpdateCommands.RemoveComponent<SearchingForDestination>(entity);
                    }
                }
                else if (chunk.Has(killedType))
                {
                    var killedArray = chunk.GetNativeArray(killedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = killedArray[entityIndex].This;

                        if (!EntityManager.HasComponent<SearchingForDestination>(entity) || m_RemoveSearchingForDestinationList.Contains(entity)) continue;

                        m_RemoveSearchingForDestinationList.Add(entity);

                        PostUpdateCommands.RemoveComponent<SearchingForDestination>(entity);
                    }
                }
            }

            chunkArray.Dispose();

            m_RemoveSearchingForDestinationList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_RemoveSearchingForDestinationList.IsCreated)
            {
                m_RemoveSearchingForDestinationList.Dispose();
            }
        }
    }
}