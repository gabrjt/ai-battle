﻿using Game.Components;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Systems
{
    public class TargetBufferSystem : JobComponentSystem
    {
        //[BurstCompile] // ;_;
        private struct ConsolidateJob : IJobChunk
        {
            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ComponentDataFromEntity<Dead> DeadFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Translation> PositionFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<AttackDistance> AttackDistanceFromEntity;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<TargetBuffer> TargetBufferFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var translation = PositionFromEntity[entity].Value;

                    var targetBuffer = TargetBufferFromEntity[entity];

                    if (targetBuffer.Length == 0) continue;

                    var targetBufferArray = targetBuffer.AsNativeArray();
                    var threatTargetList = new NativeList<TargetBuffer>(Allocator.Temp);

                    for (var targetBufferIndex = 0; targetBufferIndex < targetBufferArray.Length; targetBufferIndex++)
                    {
                        var target = targetBufferArray[targetBufferIndex].Value;
                        var targetPostion = PositionFromEntity[target].Value;
                        var attackDistance = AttackDistanceFromEntity[entity];
                        if (DeadFromEntity.Exists(target) || math.distance(translation, targetPostion) > attackDistance.Max * TargetBufferProxy.InternalBufferCapacity) continue;

                        threatTargetList.Add(target);
                    }

                    targetBufferArray = threatTargetList.AsArray();
                    var targetBufferSort = new NativeArray<SortData>(targetBufferArray.Length, Allocator.Temp);

                    for (int bufferIndex = 0; bufferIndex < targetBufferArray.Length; bufferIndex++)
                    {
                        var target = targetBufferArray[bufferIndex].Value;
                        var targetPosition = PositionFromEntity[target].Value;
                        targetBufferSort[bufferIndex] = new SortData
                        {
                            Target = target,
                            Translation = targetPosition
                        };
                    }

                    targetBuffer.Clear();

                    // TODO: Burst compliant sort algorithm
                    var targetBufferRange = new NativeArray<TargetBuffer>(targetBufferSort.OrderBy((data) => math.distancesq(data.Translation, translation)).Select((data) => data.Target).ToArray(), Allocator.Temp);

                    targetBuffer.AddRange(targetBufferRange);

                    targetBufferRange.Dispose();
                }
            }
        }

        private struct SortData
        {
            public TargetBuffer Target;

            public float3 Translation;
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<TargetBuffer>() },
                None = new[] { ComponentType.ReadOnly<Dead>() },
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps = new ConsolidateJob
            {
                EntityType = GetArchetypeChunkEntityType(),
                PositionFromEntity = GetComponentDataFromEntity<Translation>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                AttackDistanceFromEntity = GetComponentDataFromEntity<AttackDistance>(true),
                TargetBufferFromEntity = GetBufferFromEntity<TargetBuffer>()
            }.Schedule(m_Group, inputDeps);
        }
    }
}