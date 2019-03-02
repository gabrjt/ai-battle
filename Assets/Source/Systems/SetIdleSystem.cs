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
    public class SetIdleSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Idle>.Concurrent SetMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Character> CharacterType;

            [ReadOnly]
            public ArchetypeChunkComponentType<DestinationReached> DestinationReachedType;

            [ReadOnly]
            public Random Random;

            [ReadOnly]
            public float Time;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(CharacterType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        SetIdle(entity);
                    }
                }
                else if (chunk.Has(DestinationReachedType))
                {
                    var destinationReachedArray = chunk.GetNativeArray(DestinationReachedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = destinationReachedArray[entityIndex].This;

                        SetIdle(entity);
                    }
                }
            }

            private void SetIdle(Entity entity)
            {
                SetMap.TryAdd(entity, new Idle
                {
                    Duration = Random.NextFloat(2, 10),
                    StartTime = Time
                });
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Idle> SetMap;

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

        private NativeHashMap<Entity, Idle> m_SetMap;

        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[]
                {
                    ComponentType.Create<Idle>(),
                    ComponentType.ReadOnly<SearchingForDestination>(),
                    ComponentType.ReadOnly<Destination>(),
                    ComponentType.ReadOnly<Target>(),
                    ComponentType.ReadOnly<Dead>()
                }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<DestinationReached>() }
            });

            m_Random = new Random((uint)System.Environment.TickCount);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetMap = new NativeHashMap<Entity, Idle>(m_Group.CalculateLength(), Allocator.TempJob);

            var barrier = World.GetExistingManager<SetBarrier>();

            inputDeps = new ConsolidateJob
            {
                SetMap = m_SetMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                CharacterType = GetArchetypeChunkComponentType<Character>(true),
                DestinationReachedType = GetArchetypeChunkComponentType<DestinationReached>(true),
                Random = m_Random,
                Time = Time.time
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetMap = m_SetMap,
                CommandBuffer = barrier.CreateCommandBuffer()
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
            if (m_SetMap.IsCreated)
            {
                m_SetMap.Dispose();
            }
        }
    }
}