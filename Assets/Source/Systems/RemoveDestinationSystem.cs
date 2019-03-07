using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(FixedSimulationLogic))]
    public class RemoveDestinationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Destination>.Concurrent RemoveMap;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<Target> TargetType;
            [ReadOnly] public ArchetypeChunkComponentType<DestinationReached> DestinationReachedType;
            [ReadOnly] public ArchetypeChunkComponentType<Killed> KilledType;
            [ReadOnly] public ComponentDataFromEntity<Destination> DestinationFromEntity;
            [ReadOnly] public ComponentDataFromEntity<Dead> DeadFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(DestinationReachedType))
                {
                    var destinationReachedArray = chunk.GetNativeArray(DestinationReachedType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = destinationReachedArray[entityIndex].This;

                        if (!DestinationFromEntity.Exists(entity)) continue;

                        RemoveMap.TryAdd(entity, DestinationFromEntity[entity]);
                    }
                }
                else if (chunk.Has(KilledType))
                {
                    var killedArray = chunk.GetNativeArray(KilledType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var killer = killedArray[entityIndex].This;
                        var killed = killedArray[entityIndex].Other;

                        if (DestinationFromEntity.Exists(killer))
                        {
                            RemoveMap.TryAdd(killer, DestinationFromEntity[killer]);
                        }

                        if (DestinationFromEntity.Exists(killed))
                        {
                            RemoveMap.TryAdd(killed, DestinationFromEntity[killed]);
                        }
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly] public NativeHashMap<Entity, Destination> RemoveMap;
            [ReadOnly] public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                var entityArray = RemoveMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    EntityCommandBuffer.RemoveComponent<Destination>(entityArray[entityIndex]);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;
        private NativeHashMap<Entity, Destination> m_RemoveMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationReached>(), ComponentType.ReadOnly<Killed>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_RemoveMap = new NativeHashMap<Entity, Destination>(m_Group.CalculateLength(), Allocator.TempJob);

            var removeCommandBufferSystem = World.GetExistingManager<RemoveCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                RemoveMap = m_RemoveMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                KilledType = GetArchetypeChunkComponentType<Killed>(true),
                DestinationReachedType = GetArchetypeChunkComponentType<DestinationReached>(true),
                DestinationFromEntity = GetComponentDataFromEntity<Destination>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                RemoveMap = m_RemoveMap,
                EntityCommandBuffer = removeCommandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            removeCommandBufferSystem.AddJobHandleForProducer(inputDeps);

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
            if (m_RemoveMap.IsCreated)
            {
                m_RemoveMap.Dispose();
            }
        }
    }
}