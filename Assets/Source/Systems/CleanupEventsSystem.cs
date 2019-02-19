using Game.Components;
using Unity.Collections;
using Unity.Entities;

namespace Game.Systems
{
    [UpdateAfter(typeof(EndFrameBarrier))]
    public class CleanupEventsSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(
                new EntityArchetypeQuery
                {
                    All = new[] { ComponentType.ReadOnly<DestinationFound>() }
                }, new EntityArchetypeQuery
                {
                    All = new[] { ComponentType.ReadOnly<TargetFound>() }
                }, new EntityArchetypeQuery
                {
                    All = new[] { ComponentType.ReadOnly<Damaged>() }
                }, new EntityArchetypeQuery
                {
                    All = new[] { ComponentType.ReadOnly<Collided>() }
                });
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var destinationFoundType = GetArchetypeChunkComponentType<DestinationFound>(true);
            var targetFoundType = GetArchetypeChunkComponentType<TargetFound>(true);
            var damagedType = GetArchetypeChunkComponentType<Damaged>(true);
            var collidedType = GetArchetypeChunkComponentType<Collided>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);

                if (chunk.Has(targetFoundType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.RemoveComponent<TargetFound>(entityArray[entityIndex]);
                    }
                }
                else if (chunk.Has(destinationFoundType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.RemoveComponent<DestinationFound>(entityArray[entityIndex]);
                    }
                }
                else if (chunk.Has(damagedType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.AddComponent(entityArray[entityIndex], new Destroy());
                    }
                }
                else if (chunk.Has(collidedType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.AddComponent(entityArray[entityIndex], new Destroy());
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}