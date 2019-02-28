using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    //[DisableAutoCreation]
    public class SetDeadSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Dead>.Concurrent SetDeadMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Health> HealthType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Killed> KilledType;

            [ReadOnly]
            public ComponentDataFromEntity<Dead> DeadFromEntity;

            public float Time;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(HealthType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);
                    var healthArray = chunk.GetNativeArray(HealthType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var health = healthArray[entityIndex];

                        if (health.Value > 0) continue;

                        SetDeadMap.TryAdd(entity, new Dead
                        {
                            Duration = 5,
                            StartTime = Time
                        });
                    }
                }
                else if (chunk.Has(KilledType))
                {
                    var killedArray = chunk.GetNativeArray(KilledType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = killedArray[entityIndex].Other;

                        if (DeadFromEntity.Exists(entity)) continue;

                        SetDeadMap.TryAdd(entity, new Dead
                        {
                            Duration = 5,
                            StartTime = Time
                        });
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Dead> SetDeadMap;

            [ReadOnly]
            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                var entityArray = SetDeadMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    EntityCommandBuffer.AddComponent(entity, SetDeadMap[entity]);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Dead> m_SetDeadMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Health>() },
                None = new[] { ComponentType.Create<Dead>() }
            },
            new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<Killed>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_SetDeadMap.IsCreated)
            {
                m_SetDeadMap.Dispose();
            }

            m_SetDeadMap = new NativeHashMap<Entity, Dead>(m_Group.CalculateLength(), Allocator.TempJob);

            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateJob
            {
                SetDeadMap = m_SetDeadMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                HealthType = GetArchetypeChunkComponentType<Health>(true),
                KilledType = GetArchetypeChunkComponentType<Killed>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                Time = Time.time
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetDeadMap = m_SetDeadMap,
                EntityCommandBuffer = barrier.CreateCommandBuffer()
            }.Schedule(inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            if (m_SetDeadMap.IsCreated)
            {
                m_SetDeadMap.Dispose();
            }
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SetDeadMap.IsCreated)
            {
                m_SetDeadMap.Dispose();
            }
        }
    }
}