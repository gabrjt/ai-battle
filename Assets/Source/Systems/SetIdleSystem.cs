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
    [UpdateInGroup(typeof(LogicGroup))]
    public class SetIdleSystem : JobComponentSystem
    {
        private struct Job : IJobChunk
        {
            [ReadOnly] public EntityCommandBuffer.Concurrent CommandBuffer;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public Random Random;
            [ReadOnly] public float Time;
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    CommandBuffer.AddComponent(m_ThreadIndex, entityArray[entityIndex], new Idle
                    {
                        Duration = Random.NextFloat(1, 5),
                        StartTime = Time,
                        IdleTimeExpiredDispatched = false
                    });
                }
            }
        }

        private ComponentGroup m_IdleGroup;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_IdleGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[]
                {
                    ComponentType.ReadWrite<Idle>(),
                    ComponentType.ReadOnly<SearchingForDestination>(),
                    ComponentType.ReadOnly<Destination>(),
                    ComponentType.ReadOnly<Target>(),
                    ComponentType.ReadOnly<Dead>()
                }
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
                Random = m_Random,
                Time = Time.time
            }.Schedule(m_IdleGroup, inputDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }
    }
}