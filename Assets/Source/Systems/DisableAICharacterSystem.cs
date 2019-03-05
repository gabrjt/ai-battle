using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.AI;

namespace Game.Systems
{
    public class DisableAICharacterSystem : JobComponentSystem, IDisposable
    {
        private struct Initialized : ISystemStateComponentData { }

        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Initialized>.Concurrent AddInitializedEntityMap;

            public NativeQueue<Entity>.Concurrent RemoveInitializedEntityQueue;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Initialized> InitializedType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                if (!chunk.Has(InitializedType))
                {
                    for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        AddInitializedEntityMap.TryAdd(entityArray[entityIndex], new Initialized());
                    }
                }
                else
                {
                    for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        RemoveInitializedEntityQueue.Enqueue(entityArray[entityIndex]);
                    }
                }
            }
        }

        private struct AddInitializedJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Initialized> AddInitializedEntityMap;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var entityArray = AddInitializedEntityMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    CommandBuffer.AddComponent(entity, AddInitializedEntityMap[entity]);
                }

                entityArray.Dispose();
            }
        }

        private struct RemoveInitializedJob : IJob
        {
            public NativeQueue<Entity> RemoveInitializedEntityQueue;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (RemoveInitializedEntityQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.RemoveComponent<Initialized>(entity);
                }
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Initialized> m_AddInitializedMap;

        private NativeQueue<Entity> m_RemoveInitializedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<NavMeshAgent>(), ComponentType.ReadWrite<CapsuleCollider>(), ComponentType.ReadOnly<Dead>() },
                None = new[] { ComponentType.ReadWrite<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Initialized>() },
                None = new[] { ComponentType.ReadWrite<NavMeshAgent>(), ComponentType.ReadWrite<CapsuleCollider>(), ComponentType.ReadOnly<Dead>() }
            });

            m_RemoveInitializedQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            DisposeMap();

            m_AddInitializedMap = new NativeHashMap<Entity, Initialized>(m_Group.CalculateLength(), Allocator.TempJob);

            var setSystem = World.GetExistingManager<SetCommandBufferSystem>();
            var removeSystem = World.GetExistingManager<RemoveCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                AddInitializedEntityMap = m_AddInitializedMap.ToConcurrent(),
                RemoveInitializedEntityQueue = m_RemoveInitializedQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>(true)
            }.Schedule(m_Group, inputDeps);

            var addInitializedDeps = new AddInitializedJob
            {
                AddInitializedEntityMap = m_AddInitializedMap,
                CommandBuffer = setSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            var removeInitializedDeps = new RemoveInitializedJob
            {
                RemoveInitializedEntityQueue = m_RemoveInitializedQueue,
                CommandBuffer = removeSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps = JobHandle.CombineDependencies(addInitializedDeps, removeInitializedDeps);

            inputDeps.Complete();

            var entityArray = m_AddInitializedMap.GetKeyArray(Allocator.Temp);

            for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                var entity = entityArray[entityIndex];
                EntityManager.GetComponentObject<NavMeshAgent>(entity).enabled = false;
                EntityManager.GetComponentObject<CapsuleCollider>(entity).enabled = false;
            }

            entityArray.Dispose();

            setSystem.AddJobHandleForProducer(inputDeps);
            removeSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            DisposeMap();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        private void DisposeMap()
        {
            if (m_AddInitializedMap.IsCreated)
            {
                m_AddInitializedMap.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeMap();

            if (m_RemoveInitializedQueue.IsCreated)
            {
                m_RemoveInitializedQueue.Dispose();
            }
        }
    }
}