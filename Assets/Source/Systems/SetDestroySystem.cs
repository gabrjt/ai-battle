using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class SetDestroySystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent SetQueue;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<Died> DiedType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var diedArray = chunk.GetNativeArray(DiedType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = diedArray[entityIndex].This;

                    SetQueue.Enqueue(entity);
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Entity> SetQueue;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (SetQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.AddComponent(entity, new Destroy());
                }
            }
        }

        private ComponentGroup m_Group;
        private NativeQueue<Entity> m_SetQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Died>() }
            });

            m_SetQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setCommandBufferSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                SetQueue = m_SetQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                DiedType = GetArchetypeChunkComponentType<Died>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetQueue = m_SetQueue,
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer(),
            }.Schedule(inputDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();
            Dispose();
        }

        public void Dispose()
        {
            if (m_SetQueue.IsCreated)
            {
                m_SetQueue.Dispose();
            }
        }
    }
}