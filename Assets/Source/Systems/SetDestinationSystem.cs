using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    public class SetDestinationSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationFound>(), ComponentType.ReadOnly<TargetFound>() }
            });
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

                        if (!EntityManager.Exists(targetFound.Value)) continue;

                        var destination = EntityManager.GetComponentData<Position>(targetFound.Value).Value;

                        if (EntityManager.HasComponent<Destination>(targetFound.This))
                        {
                            PostUpdateCommands.SetComponent(targetFound.This, new Destination { Value = destination });
                        }
                        else
                        {
                            PostUpdateCommands.AddComponent(targetFound.This, new Destination { Value = destination });
                        }
                    }
                }
                else if (chunk.Has(destinationFoundType))
                {
                    var destinationFoundArray = chunk.GetNativeArray(destinationFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var destinationFound = destinationFoundArray[entityIndex];
                        PostUpdateCommands.AddComponent(destinationFound.This, new Destination { Value = destinationFound.Value });
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}