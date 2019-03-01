using Game.Components;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    public class SetIdleSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_SetIdleList;

        private MRandom m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[]
                {
                    ComponentType.Create<Idle>(),
                    ComponentType.ReadOnly<SearchingForDestination>(),
                    ComponentType.ReadOnly<Destination>(),
                    ComponentType.ReadOnly<Target>(),
                    ComponentType.ReadOnly<Dead>()
                }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<DestinationReached>() }
            });

            m_SetIdleList = new NativeList<Entity>(Allocator.Persistent);

            m_Random = new MRandom((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var characterType = GetArchetypeChunkComponentType<Character>(true);
            var destinationReachedType = GetArchetypeChunkComponentType<DestinationReached>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(characterType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        if (m_SetIdleList.Contains(entity)) continue;

                        m_SetIdleList.Add(entity);

                        PostUpdateCommands.AddComponent(entity, new Idle
                        {
                            Duration = m_Random.NextFloat(2, 10),
                            StartTime = Time.time
                        });
                    }
                }
                else if (chunk.Has(destinationReachedType))
                {
                    var destinationReachedArray = chunk.GetNativeArray(destinationReachedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = destinationReachedArray[entityIndex].This;

                        if (EntityManager.HasComponent<Idle>(entity) || m_SetIdleList.Contains(entity)) continue;

                        m_SetIdleList.Add(entity);

                        PostUpdateCommands.AddComponent(entity, new Idle
                        {
                            Duration = m_Random.NextFloat(2, 10),
                            StartTime = Time.time
                        });
                    }
                }
            }

            chunkArray.Dispose();

            m_SetIdleList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SetIdleList.IsCreated)
            {
                m_SetIdleList.Dispose();
            }
        }
    }
}