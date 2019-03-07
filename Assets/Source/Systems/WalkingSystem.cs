using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(FixedSimulationLogic))]
    public class WalkingSystem : JobComponentSystem
    {
        private struct Initialized : ISystemStateComponentData { }

        private struct AddWalkingJob : IJobChunk
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
                        CommandBuffer.AddComponent(m_ThreadIndex, entityArray[entityIndex], new Walking());
                    }
                }
            }
        }

        private struct RemoveWalkingJob : IJobChunk
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
                        CommandBuffer.RemoveComponent<Walking>(m_ThreadIndex, entityArray[entityIndex]);
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
                None = new[] { ComponentType.ReadWrite<Initialized>(), ComponentType.ReadWrite<Walking>(), ComponentType.ReadOnly<Target>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Initialized>(), ComponentType.ReadWrite<Walking>() },
                None = new[] { ComponentType.ReadOnly<Destination>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Initialized>(), ComponentType.ReadWrite<Walking>(), ComponentType.ReadOnly<Target>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setCommandBufferSystem = World.Active.GetExistingManager<SetCommandBufferSystem>();
            var removeCommandBufferSystem = World.Active.GetExistingManager<RemoveCommandBufferSystem>();

            var addWalkingDeps = new AddWalkingJob
            {
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>(true)
            }.Schedule(m_Group, inputDeps);

            var removeWalkingDeps = new RemoveWalkingJob
            {
                CommandBuffer = removeCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = JobHandle.CombineDependencies(addWalkingDeps, removeWalkingDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);
            removeCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}