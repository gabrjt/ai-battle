﻿using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    public class ViewVisibleSystem : JobComponentSystem, IDisposable
    {
        private struct Initialized : ISystemStateComponentData { }

        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent AddInitializedEntityQueue;

            public NativeQueue<Entity>.Concurrent RemoveInitializedEntityQueue;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Initialized> InitializedType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var initialized = chunk.Has(InitializedType);

                if (!initialized)
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        AddInitializedEntityQueue.Enqueue(entityArray[entityIndex]);
                    }
                }
                else
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        RemoveInitializedEntityQueue.Enqueue(entityArray[entityIndex]);
                    }
                }
            }
        }

        private struct AddInitializedJob : IJob
        {
            public NativeQueue<Entity> AddInitializedEntityQueue;

            public NativeList<Entity> SetEnabledTrueEntityList;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (AddInitializedEntityQueue.TryDequeue(out var entity))
                {
                    SetEnabledTrueEntityList.Add(entity);
                    CommandBuffer.AddComponent(entity, new Initialized());
                }
            }
        }

        private struct RemoveInitializedJob : IJob
        {
            public NativeQueue<Entity> RemoveInitializedEntityQueue;

            public NativeList<Entity> SetEnabledFalseEntityList;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (RemoveInitializedEntityQueue.TryDequeue(out var entity))
                {
                    SetEnabledFalseEntityList.Add(entity);
                    CommandBuffer.RemoveComponent<Initialized>(entity);
                }
            }
        }

        private NativeQueue<Entity> m_AddInitializedEntityQueue;

        private NativeQueue<Entity> m_RemoveInitializedEntityQueue;

        private NativeList<Entity> m_SetEnabledTrueEntityList;

        private NativeList<Entity> m_SetEnabledFalseEntityList;

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<View>(), ComponentType.ReadOnly<Visible>(), ComponentType.ReadOnly<Owner>() },
                None = new[] { ComponentType.ReadWrite<Initialized>(), ComponentType.ReadOnly<Destroy>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Visible>() }
            });

            m_AddInitializedEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_RemoveInitializedEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_SetEnabledTrueEntityList = new NativeList<Entity>(Allocator.Persistent);
            m_SetEnabledFalseEntityList = new NativeList<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setSystem = World.GetExistingManager<SetCommandBufferSystem>();
            var removeSystem = World.GetExistingManager<RemoveCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                AddInitializedEntityQueue = m_AddInitializedEntityQueue.ToConcurrent(),
                RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>()
            }.Schedule(m_Group, inputDeps);

            var addInitializedDeps = new AddInitializedJob
            {
                AddInitializedEntityQueue = m_AddInitializedEntityQueue,
                SetEnabledTrueEntityList = m_SetEnabledTrueEntityList,
                CommandBuffer = setSystem.CreateCommandBuffer(),
            }.Schedule(inputDeps);

            var removeInitializedDeps = new RemoveInitializedJob
            {
                RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue,
                SetEnabledFalseEntityList = m_SetEnabledFalseEntityList,
                CommandBuffer = removeSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps = JobHandle.CombineDependencies(addInitializedDeps, removeInitializedDeps);

            inputDeps.Complete();

            for (int entityIndex = 0; entityIndex < m_SetEnabledTrueEntityList.Length; entityIndex++)
            {
                SetEnabledSkinnedMeshRenderers(m_SetEnabledTrueEntityList[entityIndex], true);
            }

            m_SetEnabledTrueEntityList.Clear();

            for (int entityIndex = 0; entityIndex < m_SetEnabledFalseEntityList.Length; entityIndex++)
            {
                SetEnabledSkinnedMeshRenderers(m_SetEnabledFalseEntityList[entityIndex], false);
            }

            m_SetEnabledFalseEntityList.Clear();

            setSystem.AddJobHandleForProducer(inputDeps);
            removeSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        private void SetEnabledSkinnedMeshRenderers(Entity entity, bool enabled)
        {
            if (!EntityManager.HasComponent<Transform>(entity)) return;

            var gameObject = EntityManager.GetComponentObject<Transform>(entity).gameObject;

            var meshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                meshRenderer.enabled = enabled;
            }

            gameObject.GetComponentInChildren<Animator>().enabled = enabled;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            if (m_AddInitializedEntityQueue.IsCreated)
            {
                m_AddInitializedEntityQueue.Dispose();
            }

            if (m_RemoveInitializedEntityQueue.IsCreated)
            {
                m_RemoveInitializedEntityQueue.Dispose();
            }

            if (m_SetEnabledTrueEntityList.IsCreated)
            {
                m_SetEnabledTrueEntityList.Dispose();
            }

            if (m_SetEnabledFalseEntityList.IsCreated)
            {
                m_SetEnabledFalseEntityList.Dispose();
            }
        }
    }
}