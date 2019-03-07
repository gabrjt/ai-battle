using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(FixedSimulationLogic))]
    public class MovingSystem : JobComponentSystem
    {
        private struct Initialized : ISystemStateComponentData { }

        private struct AddMovingJob : IJobChunk
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<Initialized> InitializedType;
            private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                if (!chunk.Has(InitializedType))
                {
                    for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        CommandBuffer.AddComponent(m_ThreadIndex, entityArray[entityIndex], new Initialized());
                        CommandBuffer.AddComponent(m_ThreadIndex, entityArray[entityIndex], new Velocity());
                        CommandBuffer.AddComponent(m_ThreadIndex, entityArray[entityIndex], new MovementDirection());
                    }
                }
            }
        }

        private struct RemoveMovingJob : IJobChunk
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<Initialized> InitializedType;
            private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                if (chunk.Has(InitializedType))
                {
                    for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        CommandBuffer.RemoveComponent<Initialized>(m_ThreadIndex, entityArray[entityIndex]);
                        CommandBuffer.RemoveComponent<Velocity>(m_ThreadIndex, entityArray[entityIndex]);
                        CommandBuffer.RemoveComponent<MovementDirection>(m_ThreadIndex, entityArray[entityIndex]);
                    }
                }
            }
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();
            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destination>() },
                Any = new[] { ComponentType.ReadOnly<Walking>() },
                None = new[] { ComponentType.ReadWrite<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Destination>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setCommandBufferSystem = World.Active.GetExistingManager<SetCommandBufferSystem>();
            var removeCommandBufferSystem = World.Active.GetExistingManager<RemoveCommandBufferSystem>();

            var addMovingDeps = new AddMovingJob
            {
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>(true)
            }.Schedule(m_Group, inputDeps);

            var removeMovingDeps = new RemoveMovingJob
            {
                CommandBuffer = removeCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = JobHandle.CombineDependencies(addMovingDeps, removeMovingDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);
            removeCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}