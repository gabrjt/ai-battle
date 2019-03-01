﻿using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class RemoveDestinationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Destination>.Concurrent RemoveDestinationMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Target> TargetType;

            [ReadOnly]
            public ArchetypeChunkComponentType<DestinationReached> DestinationReachedType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Killed> KilledType;

            [ReadOnly]
            public ComponentDataFromEntity<Destination> DestinationFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Dead> DeadFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Destroy> DestroyFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(TargetType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);
                    var targetArray = chunk.GetNativeArray(TargetType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var target = targetArray[entityIndex];

                        if (!DeadFromEntity.Exists(target.Value) && !DestroyFromEntity.Exists(target.Value)) continue;

                        RemoveDestinationMap.TryAdd(entity, DestinationFromEntity[entity]);
                    }
                }

                if (chunk.Has(DestinationReachedType))
                {
                    var destinationReachedArray = chunk.GetNativeArray(DestinationReachedType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = destinationReachedArray[entityIndex].This;

                        if (!DestinationFromEntity.Exists(entity)) continue;

                        RemoveDestinationMap.TryAdd(entity, DestinationFromEntity[entity]);
                    }
                }

                if (chunk.Has(KilledType))
                {
                    var killedArray = chunk.GetNativeArray(KilledType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = killedArray[entityIndex].This;

                        if (!DestinationFromEntity.Exists(entity)) continue;

                        RemoveDestinationMap.TryAdd(entity, DestinationFromEntity[entity]);
                    }
                }

                var deadEntityArray = chunk.GetNativeArray(EntityType);
                for (int entityIndex = 0; entityIndex < deadEntityArray.Length; entityIndex++)
                {
                    var entity = deadEntityArray[entityIndex];

                    if (!DeadFromEntity.Exists(entity)) continue;

                    RemoveDestinationMap.TryAdd(entity, DestinationFromEntity[entity]);
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Destination> RemoveDestinationMap;

            [ReadOnly]
            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                var entityArray = RemoveDestinationMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    EntityCommandBuffer.RemoveComponent<Destination>(entityArray[entityIndex]);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Destination> m_RemoveDestinationMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destination>() },
                Any = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationReached>(), ComponentType.ReadOnly<Killed>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_RemoveDestinationMap.IsCreated)
            {
                m_RemoveDestinationMap.Dispose();
            }

            m_RemoveDestinationMap = new NativeHashMap<Entity, Destination>(m_Group.CalculateLength(), Allocator.TempJob);

            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateJob
            {
                RemoveDestinationMap = m_RemoveDestinationMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                KilledType = GetArchetypeChunkComponentType<Killed>(true),
                DestinationReachedType = GetArchetypeChunkComponentType<DestinationReached>(true),
                DestinationFromEntity = GetComponentDataFromEntity<Destination>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                DestroyFromEntity = GetComponentDataFromEntity<Destroy>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                RemoveDestinationMap = m_RemoveDestinationMap,
                EntityCommandBuffer = barrier.CreateCommandBuffer()
            }.Schedule(inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            Dispose();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        public void Dispose()
        {
            if (m_RemoveDestinationMap.IsCreated)
            {
                m_RemoveDestinationMap.Dispose();
            }
        }
    }
}