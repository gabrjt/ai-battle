using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class WalkSystem : JobComponentSystem, IDisposable
    {
        private struct Initialized : ISystemStateComponentData { }

        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent AddWalkingQueue;
            public NativeQueue<Entity>.Concurrent RemoveWalkingQueue;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<Initialized> InitializedType;
            [ReadOnly] public ArchetypeChunkComponentType<Target> TargetType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var initialized = chunk.Has(InitializedType);
                var entityArray = chunk.GetNativeArray(EntityType);

                if (!initialized)
                {
                    for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        AddWalkingQueue.Enqueue(entityArray[entityIndex]);
                    }
                }
                else
                {
                    for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        RemoveWalkingQueue.Enqueue(entityArray[entityIndex]);
                    }
                }
            }
        }

        private struct AddWalkingJob : IJob
        {
            public NativeQueue<Entity> AddWalkingQueue;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (AddWalkingQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.AddComponent(entity, new Walking());
                }
            }
        }

        private struct RemoveWalkingJob : IJob
        {
            public NativeQueue<Entity> RemoveWalkingQueue;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (RemoveWalkingQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<Walking>(entity);
                }
            }
        }

        private ComponentGroup m_Group;
        private NativeQueue<Entity> m_AddWalkingQueue;
        private NativeQueue<Entity> m_RemoveWalkingQueue;

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

            m_AddWalkingQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_RemoveWalkingQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setCommandBufferSystem = World.Active.GetExistingManager<SetCommandBufferSystem>();
            var removeCommandBufferSystem = World.Active.GetExistingManager<RemoveCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                AddWalkingQueue = m_AddWalkingQueue.ToConcurrent(),
                RemoveWalkingQueue = m_RemoveWalkingQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>(true),
                TargetType = GetArchetypeChunkComponentType<Target>(true)
            }.Schedule(m_Group, inputDeps);

            var addWalkingDeps = new AddWalkingJob
            {
                AddWalkingQueue = m_AddWalkingQueue,
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            var removeWalkingDeps = new RemoveWalkingJob
            {
                RemoveWalkingQueue = m_RemoveWalkingQueue,
                CommandBuffer = removeCommandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps = JobHandle.CombineDependencies(addWalkingDeps, removeWalkingDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);
            removeCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();
            Dispose();
        }

        public void Dispose()
        {
            if (m_AddWalkingQueue.IsCreated)
            {
                m_AddWalkingQueue.Dispose();
            }

            if (m_RemoveWalkingQueue.IsCreated)
            {
                m_RemoveWalkingQueue.Dispose();
            }
        }
    }
}