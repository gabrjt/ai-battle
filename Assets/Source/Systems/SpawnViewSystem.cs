using Game.Components;
using Game.Enums;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SpawnViewSystem : JobComponentSystem, IDisposable
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

                var hasInitialized = chunk.Has(InitializedType);
                var hasKnight = chunk.Has(KnightType);
                var hasOrcWolfRider = chunk.Has(OrcWolfRiderType);
                var hasSkeleton = chunk.Has(SkeletonType);

                var viewType = default(ViewType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    if (!hasInitialized)
                    {
                        AddInitializedEntityQueue.Enqueue(entity);

                        if (hasKnight)
                        {
                            viewType = ViewType.Knight;
                        }
                        else if (hasOrcWolfRider)
                        {
                            viewType = ViewType.OrcWolfRider;
                        }
                        else if (hasSkeleton)
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

        [BurstCompile]
        private struct SetDataJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Entity> EntityArray;

            [ReadOnly]
            public NativeArray<SetData> SetDataArray;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Owner> OwnerFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Translation> PositionFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Rotation> RotationFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<View> ViewFromEntity;

            public void Execute(int index)
            {
                var entity = EntityArray[index];
                var setData = SetDataArray[index];

                OwnerFromEntity[entity] = setData.Owner;
                PositionFromEntity[entity] = setData.Translation;
                RotationFromEntity[entity] = setData.Rotation;
                ViewFromEntity[entity] = setData.View;
            }
        }

        private struct AddViewReferenceJob : IJob
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<SetData> SetDataArray;

            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                for (int entityIndex = 0; entityIndex < EntityArray.Length; entityIndex++)
                {
                    EntityCommandBuffer.AddComponent(SetDataArray[entityIndex].Owner.Value, new ViewReference { Value = EntityArray[entityIndex] });
                }
            }
        }

        private struct AddInitializedJob : IJob
        {
            public NativeQueue<Entity> AddInitializedEntityQueue;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (AddInitializedEntityQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.AddComponent(entity, new Initialized());
                }
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

        private struct SpawnData
        {
            public Entity Owner;

            public ViewType ViewType;
        }

        private struct SetData
        {
            public Owner Owner;

            public Translation Translation;

            public Rotation Rotation;

            public View View;
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
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Rotation>() },
                Any = new[] { ComponentType.ReadOnly<Knight>(), ComponentType.ReadOnly<OrcWolfRider>(), ComponentType.ReadOnly<Skeleton>() },
                None = new[] { ComponentType.ReadWrite<Initialized>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Rotation>() }
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
            if (!HasSingleton<CameraSingleton>() || !EntityManager.Exists(GetSingleton<CameraSingleton>().Owner)) return inputDeps; // TODO: remove this when RequireSingletonForUpdate is working.

            var setSystem = World.GetExistingManager<SetCommandBufferSystem>();
            var removeSystem = World.GetExistingManager<RemoveCommandBufferSystem>();

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

            var addInitializedDeps = new AddInitializedJob
            {
                AddInitializedEntityQueue = m_AddInitializedEntityQueue,
                CommandBuffer = setSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            var removeInitializedDeps = new RemoveInitializedJob
            {
                RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue,
                CommandBuffer = removeSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps = JobHandle.CombineDependencies(addInitializedDeps, removeInitializedDeps);

            inputDeps.Complete();

            var entityArray = new NativeArray<Entity>(m_SpawnDataQueue.Count, Allocator.TempJob);
            var setDataArray = new NativeArray<SetData>(m_SpawnDataQueue.Count, Allocator.TempJob);

            var destroySystem = World.GetExistingManager<DestroySystem>();
            var knightPool = destroySystem.m_KnightPool;
            var orcWolfRiderPool = destroySystem.m_OrcWolfRiderPool;
            var skeletonPool = destroySystem.m_SkeletonPool;

            var entityIndex = 0;

            while (m_SpawnDataQueue.TryDequeue(out var spawnData))
            {
                var owner = spawnData.Owner;

                var translation = EntityManager.GetComponentData<Translation>(owner).Value;
                var rotation = EntityManager.GetComponentData<Rotation>(owner).Value;

                GameObject view = null;

                switch (spawnData.ViewType)
                {
                    case ViewType.Knight:
                        Instantiate(ref view, knightPool, m_KnightPrefab);
                        break;

                    case ViewType.OrcWolfRider:
                        Instantiate(ref view, orcWolfRiderPool, m_OrvWolfRiderPrefab);
                        break;

                    case ViewType.Skeleton:
                        Instantiate(ref view, skeletonPool, m_SkeletonPrefab);
                        break;

                    default:
                        continue;
                }

                view.SetActive(true);

                var entity = view.GetComponent<GameObjectEntity>().Entity;

                view.transform.position = translation;
                view.transform.rotation = rotation;

                view.name = $"{spawnData.ViewType.ToString()} {entity.Index}";

                entityArray[entityIndex] = entity;

                var setData = new SetData
                {
                    Owner = new Owner { Value = spawnData.Owner },
                    Translation = new Translation { Value = translation },
                    Rotation = new Rotation { Value = rotation },
                    View = new View { Value = spawnData.ViewType }
                };

                setDataArray[entityIndex++] = setData;
            }

            inputDeps = new SetDataJob
            {
                EntityArray = entityArray,
                SetDataArray = setDataArray,
                OwnerFromEntity = GetComponentDataFromEntity<Owner>(),
                PositionFromEntity = GetComponentDataFromEntity<Translation>(),
                RotationFromEntity = GetComponentDataFromEntity<Rotation>(),
                ViewFromEntity = GetComponentDataFromEntity<View>()
            }.Schedule(entityArray.Length, 64, inputDeps);

            inputDeps = new AddViewReferenceJob
            {
                EntityArray = entityArray,
                SetDataArray = setDataArray,
                EntityCommandBuffer = setSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            setSystem.AddJobHandleForProducer(inputDeps);
            removeSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        private void Instantiate(ref GameObject view, Queue<GameObject> pool, GameObject prefab)
        {
            if (pool.Count > 0)
            {
                view = pool.Dequeue();
            }
            else
            {
                view = Object.Instantiate(prefab);
            }
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

            if (m_SpawnDataQueue.IsCreated)
            {
                m_SpawnDataQueue.Dispose();
            }
        }
    }
}