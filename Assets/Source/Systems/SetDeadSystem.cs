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
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> EntityArray;
            [ReadOnly] public ArchetypeChunkComponentType<KillAllCharacters> KillAllCharacterType;
            [ReadOnly] public ArchetypeChunkComponentType<Killed> KilledType;
            [ReadOnly] public ComponentDataFromEntity<Dead> DeadFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Health> HealthFromEntity;
            public float Time;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(KilledType))
                {
                    var killedArray = chunk.GetNativeArray(KilledType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var killed = killedArray[entityIndex];
                        var entity = killed.Other;

                        if (DeadFromEntity.Exists(entity)) continue;

                        SetDead(entity);
                    }
                }
                else if (chunk.Has(KillAllCharacterType))
                {
                    for (var entityIndex = 0; entityIndex < EntityArray.Length; entityIndex++)
                    {
                        var entity = EntityArray[entityIndex];

                        if (DeadFromEntity.Exists(entity)) continue;

                        SetDead(entity);
                    }
                }
            }

            private void SetDead(Entity entity)
            {
                SetMap.TryAdd(entity, new Dead
                {
                    Duration = 1,
                    StartTime = Time,
                    DiedDispatched = false
                });

                HealthFromEntity[entity] = new Health { Value = 0 };
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeHashMap<Entity, Dead> SetMap;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var entityArray = SetMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    CommandBuffer.AddComponent(entity, SetMap[entity]);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;
        private ComponentGroup m_AliveCharacterGroup;
        private ComponentGroup m_CharacterGroup;
        private NativeHashMap<Entity, Dead> m_SetMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Components.Event>() },
                Any = new[] { ComponentType.ReadOnly<KillAllCharacters>(), ComponentType.ReadOnly<Killed>() }
            });

            m_AliveCharacterGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<Dead>() }
            });

            m_CharacterGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetMap = new NativeHashMap<Entity, Dead>(m_CharacterGroup.CalculateLength(), Allocator.TempJob);
            var setCommandBufferSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                SetMap = m_SetMap.ToConcurrent(),
                EntityArray = m_AliveCharacterGroup.ToEntityArray(Allocator.TempJob),
                KillAllCharacterType = GetArchetypeChunkComponentType<KillAllCharacters>(true),
                KilledType = GetArchetypeChunkComponentType<Killed>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                HealthFromEntity = GetComponentDataFromEntity<Health>(),
                Time = Time.time
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetMap = m_SetMap,
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer()
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