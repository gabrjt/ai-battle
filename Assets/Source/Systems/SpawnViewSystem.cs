﻿using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class SpawnViewSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent AddInitializedEntityQueue;

            public NativeQueue<Entity>.Concurrent RemoveInitializedEntityQueue;

            public NativeQueue<SpawnData>.Concurrent SpawnDataQueue;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Initialized> InitializedType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Knight> KnightType;

            [ReadOnly]
            public ArchetypeChunkComponentType<OrcWolfRider> OrcWolfRiderType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Skeleton> SkeletonType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                var initialized = chunk.Has(InitializedType);
                var isKnight = chunk.Has(KnightType);
                var isOrcWolfRider = chunk.Has(OrcWolfRiderType);
                var isSkeleton = chunk.Has(SkeletonType);
                var viewType = default(ViewType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    if (!initialized)
                    {
                        AddInitializedEntityQueue.Enqueue(entity);

                        if (isKnight)
                        {
                            viewType = ViewType.Knight;
                        }
                        else if (isOrcWolfRider)
                        {
                            viewType = ViewType.OrcWolfRider;
                        }
                        else if (isSkeleton)
                        {
                            viewType = ViewType.Skeleton;
                        }

                        SpawnDataQueue.Enqueue(new SpawnData
                        {
                            Owner = entity,
                            ViewType = viewType
                        });
                    }
                    else
                    {
                        RemoveInitializedEntityQueue.Enqueue(entity);
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Entity> AddInitializedEntityQueue;

            public NativeQueue<Entity> RemoveInitializedEntityQueue;

            [ReadOnly]
            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                while (AddInitializedEntityQueue.TryDequeue(out var entity))
                {
                    EntityCommandBuffer.AddComponent(entity, new Initialized());
                }

                while (RemoveInitializedEntityQueue.TryDequeue(out var entity))
                {
                    EntityCommandBuffer.RemoveComponent<Initialized>(entity);
                }
            }
        }

        private enum ViewType
        {
            Knight,
            OrcWolfRider,
            Skeleton
        }

        private struct SpawnData
        {
            public Entity Owner;

            public ViewType ViewType;
        }

        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        private GameObject m_KnightPrefab;

        private GameObject m_OrvWolfRiderPrefab;

        private GameObject m_SkeletonPrefab;

        private NativeQueue<Entity> m_AddInitializedEntityQueue;

        private NativeQueue<Entity> m_RemoveInitializedEntityQueue;

        private NativeQueue<SpawnData> m_SpawnDataQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<Rotation>() },
                Any = new[] { ComponentType.ReadOnly<Knight>(), ComponentType.ReadOnly<OrcWolfRider>(), ComponentType.ReadOnly<Skeleton>() },
                None = new[] { ComponentType.Create<Initialized>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.Create<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<Rotation>() }
            });

            Debug.Assert(m_KnightPrefab = Resources.Load<GameObject>("Knight"));
            Debug.Assert(m_OrvWolfRiderPrefab = Resources.Load<GameObject>("Orc Wolf Rider"));
            Debug.Assert(m_SkeletonPrefab = Resources.Load<GameObject>("Skeleton"));

            m_AddInitializedEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_RemoveInitializedEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_SpawnDataQueue = new NativeQueue<SpawnData>(Allocator.Persistent);

            RequireSingletonForUpdate<CameraSingleton>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (!HasSingleton<CameraSingleton>()) return inputDeps; // TODO: remove this when RequireSingletonForUpdate is working.

            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateJob
            {
                AddInitializedEntityQueue = m_AddInitializedEntityQueue.ToConcurrent(),
                RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue.ToConcurrent(),
                SpawnDataQueue = m_SpawnDataQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>(),
                KnightType = GetArchetypeChunkComponentType<Knight>(true),
                OrcWolfRiderType = GetArchetypeChunkComponentType<OrcWolfRider>(true),
                SkeletonType = GetArchetypeChunkComponentType<Skeleton>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                AddInitializedEntityQueue = m_AddInitializedEntityQueue,
                RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue,
                EntityCommandBuffer = barrier.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps.Complete();

            while (m_SpawnDataQueue.TryDequeue(out var spawnData))
            {
                var owner = spawnData.Owner;

                var position = EntityManager.GetComponentData<Position>(owner).Value;
                var rotation = EntityManager.GetComponentData<Rotation>(owner).Value;

                GameObject view;

                switch (spawnData.ViewType)
                {
                    case ViewType.Knight:
                        view = Object.Instantiate(m_KnightPrefab, position, rotation);
                        break;

                    case ViewType.OrcWolfRider:
                        view = Object.Instantiate(m_OrvWolfRiderPrefab, position, rotation);
                        break;

                    case ViewType.Skeleton:
                        view = Object.Instantiate(m_SkeletonPrefab, position, rotation);
                        break;

                    default:
                        continue;
                }

                var entity = view.GetComponent<GameObjectEntity>().Entity;

                view.name = $"{spawnData.ViewType.ToString()} {entity.Index}";

                EntityManager.SetComponentData(entity, new Owner { Value = spawnData.Owner });

                EntityManager.SetComponentData(entity, new Position { Value = position });
                EntityManager.SetComponentData(entity, new Rotation { Value = rotation });

                EntityManager.AddComponentData(owner, new ViewReference { Value = entity });
            }

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_AddInitializedEntityQueue.IsCreated)
            {
                m_AddInitializedEntityQueue.Dispose();
            }

            if (m_RemoveInitializedEntityQueue.IsCreated)
            {
                m_RemoveInitializedEntityQueue.Dispose();
            }

            if (m_SpawnDataQueue.IsCreated)
            {
                m_SpawnDataQueue.Dispose();
            }
        }
    }
}