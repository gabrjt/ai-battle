using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class SetTargetSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Target>.Concurrent SetTargetMap;

            [ReadOnly]
            public ArchetypeChunkComponentType<TargetFound> TargetFoundType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Damaged> DamagedType;

            [ReadOnly]
            public ComponentDataFromEntity<Dead> DeadFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Destroy> DestroyFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Target> TargetFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(TargetFoundType))
                {
                    var targetFoundArray = chunk.GetNativeArray(TargetFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var targetFound = targetFoundArray[entityIndex];
                        var entity = targetFound.This;
                        var target = targetFound.Other;

                        if (DeadFromEntity.Exists(target) || DestroyFromEntity.Exists(target)) continue;

                        SetTargetMap.TryAdd(entity, new Target { Value = target });
                    }
                }
                else if (chunk.Has(DamagedType))
                {
                    var damagedArray = chunk.GetNativeArray(DamagedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var damaged = damagedArray[entityIndex];
                        var entity = damaged.Other;
                        var target = damaged.This;

                        if ((DeadFromEntity.Exists(target) || DestroyFromEntity.Exists(target)) || TargetFromEntity.Exists(entity)) continue;

                        SetTargetMap.TryAdd(entity, new Target { Value = target });
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Target> SetTargetMap;

            [ReadOnly]
            public EntityCommandBuffer EntityCommandBuffer;

            public ComponentDataFromEntity<Target> TargetFromEntity;

            public void Execute()
            {
                var entityArray = SetTargetMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    if (TargetFromEntity.Exists(entity)) continue;

                    EntityCommandBuffer.AddComponent(entity, SetTargetMap[entity]);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Target> m_SetTargetMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<TargetFound>(), ComponentType.ReadOnly<Damaged>() },
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetTargetMap = new NativeHashMap<Entity, Target>(m_Group.CalculateLength(), Allocator.TempJob);

            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateJob
            {
                SetTargetMap = m_SetTargetMap.ToConcurrent(),
                TargetFoundType = GetArchetypeChunkComponentType<TargetFound>(true),
                DamagedType = GetArchetypeChunkComponentType<Damaged>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                DestroyFromEntity = GetComponentDataFromEntity<Destroy>(true),
                TargetFromEntity = GetComponentDataFromEntity<Target>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetTargetMap = m_SetTargetMap,
                EntityCommandBuffer = barrier.CreateCommandBuffer(),
                TargetFromEntity = GetComponentDataFromEntity<Target>()
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
            if (m_SetTargetMap.IsCreated)
            {
                m_SetTargetMap.Dispose();
            }
        }
    }
}