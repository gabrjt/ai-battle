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
    [UpdateInGroup(typeof(LogicGroup))]
    public class SetIdleSystem : JobComponentSystem, IDisposable
    {
        private struct SetData
        {
            public Entity Entity;
            public Idle Idle;
        }

        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<SetData>.Concurrent SetQueue;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public Random Random;
            [ReadOnly] public float Time;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    SetQueue.Enqueue(new SetData
                    {
                        Entity = entity,
                        Idle = new Idle
                        {
                            Duration = Random.NextFloat(2, 5),
                            StartTime = Time,
                            Expired = false
                        }
                    });
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<SetData> SetQueue;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (SetQueue.TryDequeue(out var data))
                {
                    CommandBuffer.AddComponent(data.Entity, data.Idle);
                    CommandBuffer.SetComponent(data.Entity, new Velocity());
                    CommandBuffer.SetComponent(data.Entity, new MovementDirection());
                }
            }
        }

        private ComponentGroup m_Group;
        private NativeQueue<SetData> m_SetQueue;

        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
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
            m_SetQueue = new NativeQueue<SetData>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                SetQueue = m_SetQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                Random = m_Random,
                Time = Time.time
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetQueue = m_SetQueue,
                CommandBuffer = setSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            setSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();
            Dispose();
        }

        public void Dispose()
        {
            if (m_SetQueue.IsCreated)
            {
                m_SetQueue.Dispose();
            }
        }
    }
}