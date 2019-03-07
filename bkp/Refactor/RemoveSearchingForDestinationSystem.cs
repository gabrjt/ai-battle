using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class RemoveSearchingForDestinationSystem : JobComponentSystem
    {
        private struct Job : IJobChunk
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<DestinationFound> DestinationFoundType;
            [NativeSetThreadIndex] public readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var destinationFoundArray = chunk.GetNativeArray(DestinationFoundType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    CommandBuffer.RemoveComponent<SearchingForDestination>(m_ThreadIndex, destinationFoundArray[entityIndex].This);
                }
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationFound>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var removeCommandBufferSystem = World.GetExistingManager<RemoveCommandBufferSystem>();

            inputDeps = new Job
            {
                CommandBuffer = removeCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                DestinationFoundType = GetArchetypeChunkComponentType<DestinationFound>(true)
            }.Schedule(m_Group, inputDeps);

            removeCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}