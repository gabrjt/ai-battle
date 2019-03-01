using Game.Components;
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
            public NativeQueue<Entity>.Concurrent AddQueue;

            public NativeQueue<Entity>.Concurrent RemoveQueue;

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
            public ArchetypeChunkComponentType<HealthBar> HealthBarType;

            [ReadOnly]
            public ArchetypeChunkComponentType<View> ViewType;

            [ReadOnly]
            public ComponentDataFromEntity<Health> HealthFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            [NativeSetThreadIndex]
            private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var maxSqrDistanceFromCameraArray = chunk.GetNativeArray(MaxSqrDistanceFromCameraType);
                var ownerArray = chunk.GetNativeArray(OwnerType);

                var hasVisible = chunk.Has(VisibleType);

                if (chunk.Has(HealthBarType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var maxSqrDistanceFromCamera = maxSqrDistanceFromCameraArray[entityIndex];
                        var owner = ownerArray[entityIndex];

                        var isVisible = HealthFromEntity.Exists(owner.Value) && HealthFromEntity[owner.Value].Value > 0 && PositionFromEntity.Exists(owner.Value) && math.distancesq(CameraPosition, PositionFromEntity[owner.Value].Value) < maxSqrDistanceFromCamera.Value;

                        SetVisible(hasVisible, entity, isVisible);
                    }
                }
                else if (chunk.Has(ViewType))
                {
                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var maxSqrDistanceFromCamera = maxSqrDistanceFromCameraArray[entityIndex];
                        var owner = ownerArray[entityIndex];

                        var isVisible = PositionFromEntity.Exists(owner.Value) && math.distancesq(CameraPosition, PositionFromEntity[owner.Value].Value) < maxSqrDistanceFromCamera.Value;

                        SetVisible(hasVisible, entity, isVisible);
                    }
                }
            }

            private void SetVisible(bool hasVisible, Entity entity, bool isVisible)
            {
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
                Any = new[] { ComponentType.ReadOnly<HealthBar>(), ComponentType.ReadOnly<View>() },
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
                AddQueue = m_AddQueue.ToConcurrent(),
                RemoveQueue = m_RemoveQueue.ToConcurrent(),
                CameraPosition = EntityManager.GetComponentObject<Transform>(GetSingleton<CameraSingleton>().Owner).parent.position, // TODO: CameraArm Entity.
                EntityType = GetArchetypeChunkEntityType(),
                MaxSqrDistanceFromCameraType = GetArchetypeChunkComponentType<MaxSqrDistanceFromCamera>(true),
                OwnerType = GetArchetypeChunkComponentType<Owner>(true),
                VisibleType = GetArchetypeChunkComponentType<Visible>(true),
                HealthBarType = GetArchetypeChunkComponentType<HealthBar>(true),
                ViewType = GetArchetypeChunkComponentType<View>(true),
                HealthFromEntity = GetComponentDataFromEntity<Health>(true),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true)
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