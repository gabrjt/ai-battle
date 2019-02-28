using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class SetTargetSystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Target>.Concurrent SetTargetMap;

            [ReadOnly]
            public ArchetypeChunkComponentType<TargetFound> TargetFoundType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Damaged> DamagedType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(TargetFoundType))
                {
                    var targetFoundArray = chunk.GetNativeArray(TargetFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var targetFound = targetFoundArray[entityIndex];
                        var entity = targetFound.This;

                        SetTargetMap.TryAdd(entity, new Target { Value = targetFound.Other });
                    }
                }
                else if (chunk.Has(TargetFoundType))
                {
                    var damagedArray = chunk.GetNativeArray(DamagedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var damaged = damagedArray[entityIndex];
                        var entity = damaged.Other;

                        SetTargetMap.TryAdd(entity, new Target { Value = damaged.This });
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Target> SetTargetMap;

            [ReadOnly]
            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                var entityArray = SetTargetMap.GetKeyArray(Allocator.Temp);

                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    EntityCommandBuffer.AddComponent(entity, SetTargetMap[entity]);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Target> m_SetTargetMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<TargetFound>(), ComponentType.ReadOnly<Damaged>() },
            });

            m_SetTargetMap = new NativeHashMap<Entity, Target>(5000, Allocator.Persistent); // TODO: externalize count;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_SetTargetMap.Clear();

            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateJob
            {
                SetTargetMap = m_SetTargetMap.ToConcurrent(),
                TargetFoundType = GetArchetypeChunkComponentType<TargetFound>(true),
                DamagedType = GetArchetypeChunkComponentType<Damaged>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetTargetMap = m_SetTargetMap,
                EntityCommandBuffer = barrier.CreateCommandBuffer()
            }.Schedule(inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SetTargetMap.IsCreated)
            {
                m_SetTargetMap.Dispose();
            }
        }
    }
}