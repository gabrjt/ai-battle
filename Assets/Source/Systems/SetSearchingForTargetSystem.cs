using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    /*
    // ONE DEPENDENCY TO RULE THEM ALL
    [UpdateBefore(typeof(SetDeadSystem))]
    [UpdateBefore(typeof(RemoveDestinationSystem))]
    [UpdateBefore(typeof(SetOwnedDestroySystem))]
    [UpdateBefore(typeof(RemoveTargetSystem))]
    [UpdateBefore(typeof(SetDestinationSystem))]
    [UpdateBefore(typeof(RemoveSearchingForTargetSystem))]
    [UpdateBefore(typeof(SetAttackDestroySystem))]
    [UpdateBefore(typeof(SetIdleSystem))]
    [UpdateBefore(typeof(RemoveSearchingForDestinationSystem))]
    [UpdateBefore(typeof(RemoveIdleSystem))]
    [UpdateBefore(typeof(SetDestroySystem))]
    */
    public class SetSearchingForTargetSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, SearchingForTarget>.Concurrent SetMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkBufferType<TargetBuffer> TargetBufferType;

            [ReadOnly]
            public float Time;

            [ReadOnly]
            public Random Random;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var targetBufferArray = chunk.GetBufferAccessor(TargetBufferType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    if (targetBufferArray[entityIndex].Length > 0) continue;

                    SetMap.TryAdd(entityArray[entityIndex], new SearchingForTarget
                    {
                        Radius = Random.NextInt(5, 11),
                        Interval = 1,
                        StartTime = Time
                    });
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, SearchingForTarget> SetMap;

            [ReadOnly]
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

        private NativeHashMap<Entity, SearchingForTarget> m_SetMap;

        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<TargetBuffer>() },
                None = new[] { ComponentType.ReadWrite<SearchingForTarget>(), ComponentType.ReadOnly<Dead>() }
            });

            m_Random = new Random((uint)Environment.TickCount);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetMap = new NativeHashMap<Entity, SearchingForTarget>(m_Group.CalculateLength(), Allocator.TempJob);

            var setSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                SetMap = m_SetMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetBufferType = GetArchetypeChunkBufferType<TargetBuffer>(true),
                Time = Time.time,
                Random = m_Random
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetMap = m_SetMap,
                CommandBuffer = setSystem.CreateCommandBuffer(),
            }.Schedule(inputDeps);

            setSystem.AddJobHandleForProducer(inputDeps);

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