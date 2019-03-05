﻿using Game.MonoBehaviours;
using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class PlayDeadSoundSystem : JobComponentSystem, IDisposable
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

            [ReadOnly]
            public ArchetypeChunkComponentType<ViewReference> ViewReferenceType;

            [ReadOnly]
            public ComponentDataFromEntity<Visible> VisibleFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                if (!chunk.Has(InitializedType) && chunk.Has(ViewReferenceType))
                {
                    var viewReferenceArray = chunk.GetNativeArray(ViewReferenceType);

                    for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        var view = viewReferenceArray[entityIndex].Value;

                        if (!VisibleFromEntity.Exists(view)) continue;

                        AddInitializedEntityMap.TryAdd(entityArray[entityIndex], new Initialized());
                    }
                }
                else
                {
                    for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        RemoveInitializedEntityQueue.Enqueue(entityArray[entityIndex]);
                    }
                }
            }
        }

        private struct AddInitializedJob : IJob
        {
            public NativeHashMap<Entity, Initialized> AddInitializedEntityMap;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var entityArray = AddInitializedEntityMap.GetKeyArray(Allocator.Temp);

                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
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

        private NativeHashMap<Entity, Initialized> m_AddInitializedEntityMap;

        private NativeQueue<Entity> m_RemoveInitializedEntityQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<ViewReference>(), ComponentType.ReadOnly<Dead>() },
                None = new[] { ComponentType.ReadWrite<Initialized>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Initialized>() },
                None = new[] { ComponentType.ReadOnly<ViewReference>(), ComponentType.ReadOnly<Dead>() },
            });

            m_RemoveInitializedEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            DisposeMap();

            m_AddInitializedEntityMap = new NativeHashMap<Entity, Initialized>(m_Group.CalculateLength(), Allocator.TempJob);

            var setSystem = World.GetExistingManager<SetCommandBufferSystem>();
            var removeSystem = World.GetExistingManager<RemoveCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                AddInitializedEntityMap = m_AddInitializedEntityMap.ToConcurrent(),
                RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>(true),
                ViewReferenceType = GetArchetypeChunkComponentType<ViewReference>(true),
                VisibleFromEntity = GetComponentDataFromEntity<Visible>(true)
            }.Schedule(m_Group, inputDeps);

            var addInitializedDeps = new AddInitializedJob
            {
                AddInitializedEntityMap = m_AddInitializedEntityMap,
                CommandBuffer = setSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            var removeInitializedDeps = new RemoveInitializedJob
            {
                RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue,
                CommandBuffer = removeSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps = JobHandle.CombineDependencies(addInitializedDeps, removeInitializedDeps);

            inputDeps.Complete();

            var viewReferenceFromEntity = GetComponentDataFromEntity<ViewReference>(true);

            var entityArray = m_AddInitializedEntityMap.GetKeyArray(Allocator.Temp);

            for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                var entity = entityArray[entityIndex];
                var transform = EntityManager.GetComponentObject<Transform>(viewReferenceFromEntity[entity].Value);

                transform.GetComponentInChildren<PlayDeadSound>().PlayAtPoint(transform.position);
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
            if (m_AddInitializedEntityMap.IsCreated)
            {
                m_AddInitializedEntityMap.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeMap();

            if (m_RemoveInitializedEntityQueue.IsCreated)
            {
                m_RemoveInitializedEntityQueue.Dispose();
            }
        }
    }
}