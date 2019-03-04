using Game.Components;
using System.Linq;
using Unity.Burst;
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
            public ComponentDataFromEntity<Position> PositionFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<AttackDistance> AttackDistanceFromEntity;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<TargetBufferElement> TargetBufferFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var position = PositionFromEntity[entity].Value;

                    var targetBuffer = TargetBufferFromEntity[entity];

                    if (targetBuffer.Length == 0) continue;

                    var targetBufferArray = targetBuffer.AsNativeArray();
                    var threatTargetList = new NativeList<TargetBufferElement>(Allocator.Temp);

                    for (var targetBufferIndex = 0; targetBufferIndex < targetBufferArray.Length; targetBufferIndex++)
                    {
                        var target = targetBufferArray[targetBufferIndex].Value;
                        var targetPostion = PositionFromEntity[target].Value;
                        var attackDistance = AttackDistanceFromEntity[entity];
                        if (DeadFromEntity.Exists(target) || math.distance(position, targetPostion) > attackDistance.Min + 0.1f * attackDistance.Max) continue;

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
                            Position = targetPosition
                        };
                    }

                    targetBuffer.Clear();

                    // TODO: Burst compliant sort algorithm 
                    var targetBufferRange = new NativeArray<TargetBufferElement>(targetBufferSort.OrderBy((data) => math.distancesq(data.Position, position)).Select((data) => data.Target).ToArray(), Allocator.Temp);

                    targetBuffer.AddRange(targetBufferRange);

                    targetBufferRange.Dispose();
                }
            }
        }

        private struct SortData
        {
            public TargetBufferElement Target;

            public float3 Position;
        }

        private ComponentGroup m_Group;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<TargetBufferElement>() },
                None = new[] { ComponentType.ReadOnly<Dead>() },
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps = new ConsolidateJob
            {
                EntityType = GetArchetypeChunkEntityType(),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                AttackDistanceFromEntity = GetComponentDataFromEntity<AttackDistance>(true),
                TargetBufferFromEntity = GetBufferFromEntity<TargetBufferElement>()
            }.Schedule(m_Group, inputDeps);
        }
    }
}