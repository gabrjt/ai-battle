﻿using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateAfter(typeof(SetCameraSingletonSystem))]
    public class SetVisibleSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            [ReadOnly]
            public float3 CameraPosition;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<MaxSqrDistanceFromCamera> MaxSqrDistanceFromCameraType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Owner> OwnerType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Visible> VisibleType;

            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            [ReadOnly]
            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            public NativeQueue<Entity>.Concurrent AddQueue;

            public NativeQueue<Entity>.Concurrent RemoveQueue;

            [NativeSetThreadIndex]
            private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var maxSqrDistanceFromCameraArray = chunk.GetNativeArray(MaxSqrDistanceFromCameraType);
                var ownerArray = chunk.GetNativeArray(OwnerType);

                var hasVisible = chunk.Has(VisibleType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var maxSqrDistanceFromCamera = maxSqrDistanceFromCameraArray[entityIndex];
                    var owner = ownerArray[entityIndex];

                    var isVisible = PositionFromEntity.Exists(owner.Value) && math.distancesq(CameraPosition, PositionFromEntity[owner.Value].Value) < math.lengthsq(maxSqrDistanceFromCamera.Value);

                    if (isVisible && !hasVisible)
                    {
                        AddQueue.Enqueue(entity);
                    }
                    else if (!isVisible && hasVisible)
                    {
                        RemoveQueue.Enqueue(entity);
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Entity> AddQueue;

            public NativeQueue<Entity> RemoveQueue;

            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                while (AddQueue.TryDequeue(out var entity))
                {
                    EntityCommandBuffer.AddComponent(entity, new Visible());
                }

                while (RemoveQueue.TryDequeue(out var entity))
                {
                    EntityCommandBuffer.RemoveComponent<Visible>(entity);
                }
            }
        }

        private ComponentGroup m_Group;

        private NativeQueue<Entity> m_AddQueue;

        private NativeQueue<Entity> m_RemoveQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_AddQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_RemoveQueue = new NativeQueue<Entity>(Allocator.Persistent);

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<MaxSqrDistanceFromCamera>(), ComponentType.ReadOnly<Owner>() },
                None = new[] { ComponentType.Create<Visible>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<MaxSqrDistanceFromCamera>(), ComponentType.ReadOnly<Owner>(), ComponentType.Create<Visible>() }
            });

            RequireSingletonForUpdate<CameraSingleton>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (!HasSingleton<CameraSingleton>()) return inputDeps; // TODO: use RequireSingletonForUpdate.

            var barrier = World.Active.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateJob
            {
                CameraPosition = EntityManager.GetComponentObject<Transform>(GetSingleton<CameraSingleton>().Owner).parent.position, // TODO: CameraArm Entity.
                EntityType = GetArchetypeChunkEntityType(),
                MaxSqrDistanceFromCameraType = GetArchetypeChunkComponentType<MaxSqrDistanceFromCamera>(true),
                OwnerType = GetArchetypeChunkComponentType<Owner>(true),
                VisibleType = GetArchetypeChunkComponentType<Visible>(true),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true),
                AddQueue = m_AddQueue.ToConcurrent(),
                RemoveQueue = m_RemoveQueue.ToConcurrent()
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            inputDeps = new ApplyJob
            {
                AddQueue = m_AddQueue,
                RemoveQueue = m_RemoveQueue,
                EntityCommandBuffer = barrier.CreateCommandBuffer(),
            }.Schedule(inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_AddQueue.IsCreated)
            {
                m_AddQueue.Dispose();
            }

            if (m_RemoveQueue.IsCreated)
            {
                m_RemoveQueue.Dispose();
            }
        }
    }
}