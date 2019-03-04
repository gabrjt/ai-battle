using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class RemoveTargetSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Target>.Concurrent RemoveMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Target> TargetType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Dead> DeadType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Killed> KilledType;

            [ReadOnly]
            public ComponentDataFromEntity<Dead> DeadFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Target> TargetFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(TargetType) && chunk.Has(DeadType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);
                    var targetArray = chunk.GetNativeArray(TargetType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var target = targetArray[entityIndex];

                        RemoveMap.TryAdd(entity, target);
                    }
                }
                else if (chunk.Has(TargetType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);
                    var targetArray = chunk.GetNativeArray(TargetType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var target = targetArray[entityIndex];

                        if (!DeadFromEntity.Exists(target.Value)) continue;

                        RemoveMap.TryAdd(entity, target);
                    }
                }
                else if (chunk.Has(KilledType))
                {
                    var killedArray = chunk.GetNativeArray(KilledType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = killedArray[entityIndex].This;
                        var target = killedArray[entityIndex].Other;

                        var targetExists = TargetFromEntity.Exists(entity);

                        if (!targetExists || (targetExists && TargetFromEntity[entity].Value != target)) continue;

                        RemoveMap.TryAdd(entity, new Target { Value = target });
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Target> RemoveTargetMap;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var entityArray = RemoveTargetMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    CommandBuffer.RemoveComponent<Target>(entity);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Target> m_RemoveTargetMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.Create<Target>() },
                None = new[] { ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.Create<Target>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Killed>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_RemoveTargetMap = new NativeHashMap<Entity, Target>(m_Group.CalculateLength(), Allocator.TempJob);

            var removeBarrier = World.GetExistingManager<RemoveBarrier>();

            inputDeps = new ConsolidateJob
            {
                RemoveMap = m_RemoveTargetMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                DeadType = GetArchetypeChunkComponentType<Dead>(true),
                KilledType = GetArchetypeChunkComponentType<Killed>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                TargetFromEntity = GetComponentDataFromEntity<Target>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                RemoveTargetMap = m_RemoveTargetMap,
                CommandBuffer = removeBarrier.CreateCommandBuffer(),
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
            if (m_RemoveTargetMap.IsCreated)
            {
                m_RemoveTargetMap.Dispose();
            }
        }
    }
}