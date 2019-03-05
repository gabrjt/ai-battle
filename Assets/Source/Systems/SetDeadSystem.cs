using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class SetDeadSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Dead>.Concurrent SetMap;
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<Health> HealthType;
            [ReadOnly] public ArchetypeChunkComponentType<KillAllCharacters> KillAllCharacterType;
            [ReadOnly] public ArchetypeChunkComponentType<Killed> KilledType;
            [ReadOnly] public ComponentDataFromEntity<Dead> DeadFromEntity;
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

                        SetMap.TryAdd(entity, new Dead
                        {
                            Duration = 1,
                            StartTime = Time,
                            Expired = false
                        });
                    }
                }
                else if (chunk.Has(KillAllCharacterType))
                {
                    for (var entityIndex = 0; entityIndex < EntityArray.Length; entityIndex++)
                    {
                        var entity = EntityArray[entityIndex];

                        if (DeadFromEntity.Exists(entity)) continue;

                        SetMap.TryAdd(entity, new Dead
                        {
                            Duration = 1,
                            StartTime = Time,
                            Expired = false
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

                        SetMap.TryAdd(entity, new Dead
                        {
                            Duration = 1,
                            StartTime = Time,
                            Expired = false
                        });
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly] public NativeHashMap<Entity, Dead> SetMap;
            public EntityCommandBuffer CommandBuffer;
            public ComponentDataFromEntity<Health> HealthFromEntity;

            public void Execute()
            {
                var entityArray = SetMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    CommandBuffer.AddComponent(entity, SetMap[entity]);
                    if (HealthFromEntity.Exists(entity))
                    {
                        HealthFromEntity[entity] = new Health { Value = 0 };
                    }
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;
        private ComponentGroup m_CharacterGroup;
        private NativeHashMap<Entity, Dead> m_SetMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Health>() },
                None = new[] { ComponentType.ReadWrite<Dead>() }
            },
            new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Components.Event>() },
                Any = new[] { ComponentType.ReadOnly<KillAllCharacters>(), ComponentType.ReadOnly<Killed>() }
            });

            m_CharacterGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<Dead>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetMap = new NativeHashMap<Entity, Dead>(m_Group.CalculateLength(), Allocator.TempJob);
            var setCommandBufferSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                SetMap = m_SetMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                EntityArray = m_CharacterGroup.ToEntityArray(Allocator.TempJob),
                HealthType = GetArchetypeChunkComponentType<Health>(true),
                KillAllCharacterType = GetArchetypeChunkComponentType<KillAllCharacters>(true),
                KilledType = GetArchetypeChunkComponentType<Killed>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                Time = Time.time
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetMap = m_SetMap,
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer(),
                HealthFromEntity = GetComponentDataFromEntity<Health>()
            }.Schedule(inputDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);

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
            if (m_SetMap.IsCreated)
            {
                m_SetMap.Dispose();
            }
        }
    }
}