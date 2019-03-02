using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Systems
{
    [UpdateAfter(typeof(SetCameraSingletonSystem))]
    [UpdateAfter(typeof(SetCanvasSingletonSystem))]
    public class SpawnHealthBarSystem : JobComponentSystem, IDisposable
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
            public ArchetypeChunkComponentType<Position> PositionType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var positionArray = chunk.GetNativeArray(PositionType);

                var initialized = chunk.Has(InitializedType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    if (!initialized)
                    {
                        AddInitializedEntityQueue.Enqueue(entity);

                        SpawnDataQueue.Enqueue(new SpawnData
                        {
                            Owner = entity,
                            Position = positionArray[entityIndex].Value
                        });
                    }
                    else
                    {
                        RemoveInitializedEntityQueue.Enqueue(entity);
                    }
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

        [BurstCompile]
        private struct SetDataJob : IJobParallelFor
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Owner> SetDataArray;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Owner> OwnerFromEntity;

            public void Execute(int index)
            {
                OwnerFromEntity[EntityArray[index]] = SetDataArray[index];
            }
        }

        private struct SpawnData
        {
            public Entity Owner;

            public float3 Position;
        }

        private struct Initialized : ISystemStateComponentData { }

        private ComponentGroup m_Group;

        private GameObject m_Prefab;

        private NativeQueue<Entity> m_AddInitializedEntityQueue;

        private NativeQueue<Entity> m_RemoveInitializedEntityQueue;

        private NativeQueue<SpawnData> m_SpawnDataQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Health>(), ComponentType.ReadOnly<MaxHealth>(), ComponentType.ReadOnly<Position>() },
                None = new[] { ComponentType.Create<Initialized>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.Create<Initialized>() },
                None = new[] { ComponentType.ReadOnly<Health>(), ComponentType.ReadOnly<MaxHealth>(), ComponentType.ReadOnly<Position>() }
            });

            Debug.Assert(m_Prefab = Resources.Load<GameObject>("Health Bar"));

            m_AddInitializedEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_RemoveInitializedEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_SpawnDataQueue = new NativeQueue<SpawnData>(Allocator.Persistent);

            RequireSingletonForUpdate<CameraSingleton>();
            RequireSingletonForUpdate<CanvasSingleton>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (!HasSingleton<CameraSingleton>() && !HasSingleton<CanvasSingleton>()) return inputDeps; // TODO: remove this when RequireSingletonForUpdate is working.

            var setBarrier = World.GetExistingManager<SetBarrier>();
            var removeBarrier = World.GetExistingManager<RemoveBarrier>();

            inputDeps = new ConsolidateJob
            {
                AddInitializedEntityQueue = m_AddInitializedEntityQueue.ToConcurrent(),
                RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue.ToConcurrent(),
                SpawnDataQueue = m_SpawnDataQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                InitializedType = GetArchetypeChunkComponentType<Initialized>(),
                PositionType = GetArchetypeChunkComponentType<Position>(true)
            }.Schedule(m_Group, inputDeps);

            var addInitializedDeps = new AddInitializedJob
            {
                AddInitializedEntityQueue = m_AddInitializedEntityQueue,
                CommandBuffer = setBarrier.CreateCommandBuffer()
            }.Schedule(inputDeps);

            var removeInitializedDeps = new RemoveInitializedJob
            {
                RemoveInitializedEntityQueue = m_RemoveInitializedEntityQueue,
                CommandBuffer = removeBarrier.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps = JobHandle.CombineDependencies(addInitializedDeps, removeInitializedDeps);

            inputDeps.Complete();

            var canvas = GetSingleton<CanvasSingleton>();
            var camera = EntityManager.GetComponentObject<Camera>(GetSingleton<CameraSingleton>().Owner);
            var canvasTransform = EntityManager.GetComponentObject<RectTransform>(canvas.Owner);

            var entityArray = new NativeArray<Entity>(m_SpawnDataQueue.Count, Allocator.TempJob);
            var ownerArray = new NativeArray<Owner>(m_SpawnDataQueue.Count, Allocator.TempJob);

            var entityIndex = 0;
            while (m_SpawnDataQueue.TryDequeue(out var spawnData))
            {
                var healthBar = Object.Instantiate(m_Prefab, canvasTransform);

                var entity = healthBar.GetComponentInChildren<GameObjectEntity>().Entity;

                healthBar.name = $"Health Bar {entity.Index}";

                var transform = healthBar.GetComponent<RectTransform>();
                transform.position = camera.WorldToScreenPoint(spawnData.Position + math.up());

                entityArray[entityIndex] = entity;
                ownerArray[entityIndex] = new Owner { Value = spawnData.Owner };
                ++entityIndex;
            }

            inputDeps = new SetDataJob
            {
                EntityArray = entityArray,
                SetDataArray = ownerArray,
                OwnerFromEntity = GetComponentDataFromEntity<Owner>()
            }.Schedule(entityArray.Length, 64, inputDeps);

            setBarrier.AddJobHandleForProducer(inputDeps);
            removeBarrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
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