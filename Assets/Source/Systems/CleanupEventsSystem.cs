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

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.RemoveComponent<TargetFound>(entityArray[entityIndex]);
                    }
                }
                else if (chunk.Has(destinationFoundType))
                {
                    var entityArray = chunk.GetNativeArray(entityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        PostUpdateCommands.RemoveComponent<DestinationFound>(entityArray[entityIndex]);
                    }
                }
            }

            chunkArray.Dispose();
        }
    }
}