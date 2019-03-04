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
            public NativeHashMap<Entity, Target>.Concurrent SetMap;

            [ReadOnly]
            public ArchetypeChunkComponentType<TargetFound> TargetFoundType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Damaged> DamagedType;

            [ReadOnly]
            public ComponentDataFromEntity<Dead> DeadFromEntity;

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

                        if (DeadFromEntity.Exists(target) || (TargetFromEntity.Exists(entity) && target == TargetFromEntity[entity].Value)) continue;

                        SetMap.TryAdd(entity, new Target { Value = target });
                    }
                }
                else if (chunk.Has(DamagedType))
                {
                    var damagedArray = chunk.GetNativeArray(DamagedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var damaged = damagedArray[entityIndex];
                        var damagedEntity = damaged.Other;
                        var hasCurrentTarget = TargetFromEntity.Exists(damagedEntity);
                        var currentTarget = hasCurrentTarget ? TargetFromEntity[damagedEntity].Value : default;
                        var newTarget = damaged.This;

                        if (DeadFromEntity.Exists(newTarget) || hasCurrentTarget && !DeadFromEntity.Exists(currentTarget)) continue;

                        SetMap.TryAdd(damagedEntity, new Target { Value = newTarget });
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Target> SetMap;

            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public ComponentDataFromEntity<Target> TargetFromEntity;

            public void Execute()
            {
                var entityArray = SetMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    if (TargetFromEntity.Exists(entity))
                    {
                        CommandBuffer.SetComponent(entity, SetMap[entity]);
                    }
                    else
                    {
                        CommandBuffer.AddComponent(entity, SetMap[entity]);
                    }
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

            var setBarrier = World.GetExistingManager<SetBarrier>();

            inputDeps = new ConsolidateJob
            {
                SetMap = m_SetTargetMap.ToConcurrent(),
                TargetFoundType = GetArchetypeChunkComponentType<TargetFound>(true),
                DamagedType = GetArchetypeChunkComponentType<Damaged>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                TargetFromEntity = GetComponentDataFromEntity<Target>(true),
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetMap = m_SetTargetMap,
                CommandBuffer = setBarrier.CreateCommandBuffer(),
                TargetFromEntity = GetComponentDataFromEntity<Target>()
            }.Schedule(inputDeps);

            setBarrier.AddJobHandleForProducer(inputDeps);

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