using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    public class SetDestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private NativeList<Entity> m_SetDestinationList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationFound>(), ComponentType.ReadOnly<TargetFound>() },
                None = new[] { ComponentType.ReadOnly<Idle>() }
            });

            m_SetDestinationList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var destinationFoundType = GetArchetypeChunkComponentType<DestinationFound>(true);
            var targetFoundType = GetArchetypeChunkComponentType<TargetFound>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];

                if (chunk.Has(targetFoundType))
                {
                    var targetFoundArray = chunk.GetNativeArray(targetFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var targetFound = targetFoundArray[entityIndex];

                        var entity = targetFound.This;

                        if (!EntityManager.Exists(targetFound.Value) || m_SetDestinationList.Contains(entity)) continue;

                        m_SetDestinationList.Add(entity);

                        var destination = EntityManager.GetComponentData<Position>(targetFound.Value).Value;
                        if (EntityManager.HasComponent<Destination>(entity))
                        {
                            var lastDestination = EntityManager.GetComponentData<Destination>(entity);
                            PostUpdateCommands.SetComponent(entity, new Destination
                            {
                                Value = destination,
                                LastValue = lastDestination.Value
                            });
                        }
                        else
                        {
                            PostUpdateCommands.AddComponent(entity, new Destination
                            {
                                Value = destination,
                                LastValue = destination
                            });
                        }
                    }
                }

                if (chunk.Has(destinationFoundType))
                {
                    var destinationFoundArray = chunk.GetNativeArray(destinationFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var destinationFound = destinationFoundArray[entityIndex];

                        var entity = destinationFound.This;

                        if (m_SetDestinationList.Contains(entity)) continue;

                        m_SetDestinationList.Add(entity);

                        PostUpdateCommands.AddComponent(entity, new Destination
                        {
                            Value = destinationFound.Value,
                            LastValue = destinationFound.Value
                        });
                    }
                }
            }

            chunkArray.Dispose();

            m_SetDestinationList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SetDestinationList.IsCreated)
            {
                m_SetDestinationList.Dispose();
            }
        }
    }
}