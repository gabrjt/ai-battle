using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Systems
{
    public class SearchForDestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeHashMap<Entity, float3> m_CalculatingPaths;

        [Inject]
        private SpawnAICharactersSystem m_SpawnAICharactersSystem;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<SearchingForDestination>() },
                None = new[] { ComponentType.ReadOnly<Idle>() }
            });

            
            m_CalculatingPaths = new NativeHashMap<Entity, float3>(m_SpawnAICharactersSystem.m_Count, Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var terrain = Terrain.activeTerrain;

            var chunks = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();

            for (var chunkIndex = 0; chunkIndex < chunks.Length; chunkIndex++)
            {
                var chunk = chunks[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var navMeshAgent = EntityManager.GetComponentObject<NavMeshAgent>(entity);

                    if (m_CalculatingPaths.TryGetValue(entity, out var destination))
                    {
                        if (navMeshAgent.pathPending) continue;

                        if (navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete)
                        {
                            m_CalculatingPaths.Remove(entity);
                            AddEntityDestinationToCalculatingPaths(terrain, entity, navMeshAgent);
                        }
                        else
                        {
                            m_CalculatingPaths.Remove(entity);
                            PostUpdateCommands.RemoveComponent<SearchingForDestination>(entity);
                            PostUpdateCommands.AddComponent(entity, new Destination { Value = destination });
                        }
                    }
                    else
                    {
                        AddEntityDestinationToCalculatingPaths(terrain, entity, navMeshAgent);
                    }
                }
            }

            chunks.Dispose();
        }

        private void AddEntityDestinationToCalculatingPaths(Terrain terrain, Entity entity, NavMeshAgent navMeshAgent)
        {
            var destination = terrain.GetRandomPosition();
            navMeshAgent.SetDestination(destination);
            m_CalculatingPaths.TryAdd(entity, destination);
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_CalculatingPaths.IsCreated)
            {
                m_CalculatingPaths.Dispose();
            }
        }
    }
}