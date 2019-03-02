using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class RemoveSearchingForTargetSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, SearchingForTarget>.Concurrent RemoveMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Target> TargetType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Dead> DeadType;

            [ReadOnly]
            public ArchetypeChunkComponentType<TargetFound> TargetFoundType;

            [ReadOnly]
            public ComponentDataFromEntity<SearchingForTarget> SearchingForTargetFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(TargetType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        RemoveMap.TryAdd(entity, new SearchingForTarget());
                    }
                }
                else if (chunk.Has(DeadType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        RemoveMap.TryAdd(entity, new SearchingForTarget());
                    }
                }
                else if (chunk.Has(TargetFoundType))
                {
                    var targetFoundArray = chunk.GetNativeArray(TargetFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = targetFoundArray[entityIndex].This;

                        if (!SearchingForTargetFromEntity.Exists(entity)) continue;

                        RemoveMap.TryAdd(entity, new SearchingForTarget());
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeHashMap<Entity, SearchingForTarget> RemoveSearchingForTargetMap;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var entityArray = RemoveSearchingForTargetMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    CommandBuffer.RemoveComponent<SearchingForTarget>(entity);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, SearchingForTarget> m_RemoveMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Target>(), ComponentType.Create<SearchingForTarget>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>() }
            },
            new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.Create<SearchingForTarget>(), ComponentType.ReadOnly<Dead>() },
                None = new[] { ComponentType.ReadOnly<Idle>(), ComponentType.ReadOnly<Destination>() }
            },
            new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<TargetFound>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_RemoveMap = new NativeHashMap<Entity, SearchingForTarget>(m_Group.CalculateLength(), Allocator.TempJob);

            var removeBarrier = World.GetExistingManager<RemoveBarrier>();

            inputDeps = new ConsolidateJob
            {
                RemoveMap = m_RemoveMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                DeadType = GetArchetypeChunkComponentType<Dead>(true),
                TargetFoundType = GetArchetypeChunkComponentType<TargetFound>(true),
                SearchingForTargetFromEntity = GetComponentDataFromEntity<SearchingForTarget>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                RemoveSearchingForTargetMap = m_RemoveMap,
                CommandBuffer = removeBarrier.CreateCommandBuffer()
            }.Schedule(inputDeps);

            removeBarrier.AddJobHandleForProducer(inputDeps);

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