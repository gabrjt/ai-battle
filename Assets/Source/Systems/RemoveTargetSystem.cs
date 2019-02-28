using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    //[DisableAutoCreation]
    public class RemoveTargetSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Target>.Concurrent RemoveTargetMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Target> TargetType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Killed> KilledType;

            [ReadOnly]
            public ComponentDataFromEntity<Dead> DeadFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Destroy> DestroyFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(TargetType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);
                    var targetArray = chunk.GetNativeArray(TargetType);
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];

                        if (DeadFromEntity.Exists(entity))
                        {
                            RemoveTargetMap.TryAdd(entity, targetArray[entityIndex]);
                        }
                        else
                        {
                            var target = targetArray[entityIndex];

                            if (!DeadFromEntity.Exists(target.Value) || !DestroyFromEntity.Exists(target.Value)) continue;

                            RemoveTargetMap.TryAdd(entity, target);
                        }
                    }
                }
                else if (chunk.Has(KilledType))
                {
                    var killedArray = chunk.GetNativeArray(KilledType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = killedArray[entityIndex].This;
                        var target = killedArray[entityIndex].Other;

                        RemoveTargetMap.TryAdd(entity, new Target { Value = target });
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;

            [ReadOnly]
            public EntityCommandBuffer EntityCommandBuffer;

            [ReadOnly]
            public ComponentDataFromEntity<Target> TargetFromEntity;

            public void Execute()
            {
                for (var entityIndex = 0; entityIndex < EntityArray.Length; entityIndex++)
                {
                    var entity = EntityArray[entityIndex];

                    if (!TargetFromEntity.Exists(entity)) continue;

                    EntityCommandBuffer.RemoveComponent<Target>(entity);
                }
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Target> m_RemoveTargetMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Target>() },
                None = new[] { ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Killed>() }
            });

            m_RemoveTargetMap = new NativeHashMap<Entity, Target>(5000, Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            if (m_RemoveTargetMap.IsCreated)
            {
                m_RemoveTargetMap.Dispose();
            }

            m_RemoveTargetMap = new NativeHashMap<Entity, Target>(m_Group.CalculateLength(), Allocator.TempJob);

            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateJob
            {
                RemoveTargetMap = m_RemoveTargetMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                KilledType = GetArchetypeChunkComponentType<Killed>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                DestroyFromEntity = GetComponentDataFromEntity<Destroy>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            inputDeps = new ApplyJob
            {
                EntityArray = m_RemoveTargetMap.GetKeyArray(Allocator.TempJob),
                EntityCommandBuffer = barrier.CreateCommandBuffer(),
                TargetFromEntity = GetComponentDataFromEntity<Target>(true)
            }.Schedule(inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            if (m_RemoveTargetMap.IsCreated)
            {
                m_RemoveTargetMap.Dispose();
            }
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_RemoveTargetMap.IsCreated)
            {
                m_RemoveTargetMap.Dispose();
            }
        }
    }
}