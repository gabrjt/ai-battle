using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class RemoveSearchingForDestinationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, SearchingForDestination>.Concurrent RemoveMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Destination> DestinationType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Dead> DeadType;

            [ReadOnly]
            public ArchetypeChunkComponentType<DestinationFound> DestinationFoundType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Killed> KilledType;

            [ReadOnly]
            public ComponentDataFromEntity<SearchingForDestination> SearchingForDestinationFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(DestinationType) || chunk.Has(DeadType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        RemoveMap.TryAdd(entity, default);
                    }
                }
                else
                {
                    if (chunk.Has(DestinationFoundType))
                    {
                        var destinationFoundArray = chunk.GetNativeArray(DestinationFoundType);

                        for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                        {
                            var entity = destinationFoundArray[entityIndex].This;

                            if (!SearchingForDestinationFromEntity.Exists(entity)) continue;

                            RemoveMap.TryAdd(entity, default);
                        }
                    }
                    else if (chunk.Has(KilledType))
                    {
                        var killedArray = chunk.GetNativeArray(KilledType);

                        for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                        {
                            var entity = killedArray[entityIndex].This;

                            if (!SearchingForDestinationFromEntity.Exists(entity)) continue;

                            RemoveMap.TryAdd(entity, default);
                        }
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, SearchingForDestination> RemoveMap;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var entityArray = RemoveMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    CommandBuffer.RemoveComponent<SearchingForDestination>(entity);
                }
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, SearchingForDestination> m_RemoveMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.Create<SearchingForDestination>(), ComponentType.ReadOnly<Destination>() },
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.Create<SearchingForDestination>(), ComponentType.ReadOnly<Dead>() },
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationFound>(), ComponentType.ReadOnly<Killed>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_RemoveMap = new NativeHashMap<Entity, SearchingForDestination>(m_Group.CalculateLength(), Allocator.TempJob);

            var barrier = World.GetExistingManager<RemoveBarrier>();

            inputDeps = new ConsolidateJob
            {
                RemoveMap = m_RemoveMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                DestinationType = GetArchetypeChunkComponentType<Destination>(true),
                DeadType = GetArchetypeChunkComponentType<Dead>(true),
                DestinationFoundType = GetArchetypeChunkComponentType<DestinationFound>(true),
                KilledType = GetArchetypeChunkComponentType<Killed>(true),
                SearchingForDestinationFromEntity = GetComponentDataFromEntity<SearchingForDestination>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                RemoveMap = m_RemoveMap,
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
            if (m_RemoveMap.IsCreated)
            {
                m_RemoveMap.Dispose();
            }
        }
    }
}