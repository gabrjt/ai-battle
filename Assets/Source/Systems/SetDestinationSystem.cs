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
                    var entityArray = chunk.GetNativeArray(entityType);
                    var targetFoundArray = chunk.GetNativeArray(targetFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var target = targetFoundArray[entityIndex].Value;
                        var destination = EntityManager.GetComponentData<Position>(target).Value;

                        if (EntityManager.HasComponent<Destination>(entity))
                        {
                            PostUpdateCommands.SetComponent(entity, new Destination { Value = destination });
                        }
                        else
                        {
                            PostUpdateCommands.AddComponent(entity, new Destination { Value = destination });
                        }
                    }
                }
                else if (chunk.Has(destinationFoundType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);
                    var destinationFoundArray = chunk.GetNativeArray(destinationFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var destination = destinationFoundArray[entityIndex].Value;
                        PostUpdateCommands.AddComponent(entity, new Destination { Value = destination });
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}