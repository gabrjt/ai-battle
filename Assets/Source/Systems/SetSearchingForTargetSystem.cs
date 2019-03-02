using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Systems
{
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
    public class SetSearchingForTargetSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, SearchingForTarget>.Concurrent SetSearchingForTargetMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public float Time;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    SetSearchingForTargetMap.TryAdd(entityArray[entityIndex], new SearchingForTarget
                    {
                        Radius = 15,
                        Interval = 1,
                        StartTime = Time
                    });
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, SearchingForTarget> SetSearchingForTargetMap;

            [ReadOnly]
            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                var entityArray = SetSearchingForTargetMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    EntityCommandBuffer.AddComponent(entity, SetSearchingForTargetMap[entity]);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, SearchingForTarget> m_SetSearchingForTargetMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                Any = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>() },
                None = new[] { ComponentType.Create<SearchingForTarget>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>(), ComponentType.ReadOnly<Destroy>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetSearchingForTargetMap = new NativeHashMap<Entity, SearchingForTarget>(m_Group.CalculateLength(), Allocator.TempJob);

            var barrier = World.GetExistingManager<SetBarrier>();

            inputDeps = new ConsolidateJob
            {
                SetSearchingForTargetMap = m_SetSearchingForTargetMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                Time = Time.time
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetSearchingForTargetMap = m_SetSearchingForTargetMap,
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
            if (m_SetSearchingForTargetMap.IsCreated)
            {
                m_SetSearchingForTargetMap.Dispose();
            }
        }
    }
}