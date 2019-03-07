using Game.Components;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class SetDestinationSystem : JobComponentSystem
    {
        private struct Job : IJobChunk
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<DestinationFound> DestinationFoundType;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var destinationFoundArray = chunk.GetNativeArray(DestinationFoundType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var destinationFound = destinationFoundArray[entityIndex];
                    var entity = destinationFound.This;

                    CommandBuffer.AddComponent(m_ThreadIndex, entity, new Destination
                    {
                        Value = destinationFound.Value,
                        LastValue = destinationFound.Value
                    });
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
            var setCommandBufferSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new Job
            {
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                DestinationFoundType = GetArchetypeChunkComponentType<DestinationFound>(true)
            }.Schedule(m_Group, inputDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}