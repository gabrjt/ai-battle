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
        private struct SetData
        {
            public Entity Entity;
            public Destroy Destroy;
        }

        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<SetData>.Concurrent SetQueue;
            [ReadOnly] public ArchetypeChunkComponentType<Died> DiedType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var diedArray = chunk.GetNativeArray(DiedType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    SetQueue.Enqueue(new SetData
                    {
                        Entity = diedArray[entityIndex].This,
                        Destroy = new Destroy()
                    });
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<SetData> SetQueue;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (SetQueue.TryDequeue(out var data))
                {
                    CommandBuffer.AddComponent(data.Entity, data.Destroy);
                }
            }
        }

        private ComponentGroup m_Group;
        private NativeQueue<SetData> m_SetQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Died>() }
            });

            m_SetQueue = new NativeQueue<SetData>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setCommandBufferSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                SetQueue = m_SetQueue.ToConcurrent(),
                DiedType = GetArchetypeChunkComponentType<Died>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetQueue = m_SetQueue,
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer()
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