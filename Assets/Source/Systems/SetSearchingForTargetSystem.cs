using Game.Components;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
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

    [UpdateInGroup(typeof(LogicGroup))]
    public class SetSearchingForTargetSystem : JobComponentSystem
    {
        private struct Job : IJobChunk
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public float Time;
            [ReadOnly] public Random Random;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    CommandBuffer.AddComponent(m_ThreadIndex, entityArray[entityIndex], new SearchingForTarget
                    {
                        Radius = Random.NextInt(5, 11),
                        Interval = 1,
                        StartTime = Time
                    });
                }
            }
        }

        private ComponentGroup m_Group;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadWrite<SearchingForTarget>(), ComponentType.ReadOnly<Dead>() }
            });

            m_Random = new Random((uint)Environment.TickCount);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setCommandBufferSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new Job
            {
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                Time = Time.time,
                Random = m_Random
            }.Schedule(m_Group, inputDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}