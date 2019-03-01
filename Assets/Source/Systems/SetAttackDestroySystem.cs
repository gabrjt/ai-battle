using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class SetAttackDestroySystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Destroy>.Concurrent SetDestroyMap;

            [ReadOnly]
            public ArchetypeChunkComponentType<Collided> CollidedType;

            [ReadOnly]
            public ArchetypeChunkComponentType<MaxDistanceReached> MaxDistanceReachedType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(CollidedType))
                {
                    var collidedArray = chunk.GetNativeArray(CollidedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = collidedArray[entityIndex].This;

                        SetDestroyMap.TryAdd(entity, new Destroy());
                    }
                }
                else if (chunk.Has(MaxDistanceReachedType))
                {
                    var maxDistanceArray = chunk.GetNativeArray(MaxDistanceReachedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = maxDistanceArray[entityIndex].This;

                        SetDestroyMap.TryAdd(entity, new Destroy());
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Destroy> SetDestroyMap;

            [ReadOnly]
            public EntityCommandBuffer EntityCommandBuffer;

            [ReadOnly]
            public ComponentDataFromEntity<Destroy> DestroyFromEntity;

            public void Execute()
            {
                var entityArray = SetDestroyMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    if (DestroyFromEntity.Exists(entity)) continue;

                    EntityCommandBuffer.AddComponent(entity, SetDestroyMap[entity]);
                    EntityCommandBuffer.AddComponent(entity, new Disabled());
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Destroy> m_SetDestroyMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<Collided>(), ComponentType.ReadOnly<MaxDistanceReached>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            inputDeps.Complete();

            m_SetDestroyMap = new NativeHashMap<Entity, Destroy>(m_Group.CalculateLength(), Allocator.TempJob);

            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateJob
            {
                SetDestroyMap = m_SetDestroyMap.ToConcurrent(),
                CollidedType = GetArchetypeChunkComponentType<Collided>(true),
                MaxDistanceReachedType = GetArchetypeChunkComponentType<MaxDistanceReached>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            inputDeps = new ApplyJob
            {
                SetDestroyMap = m_SetDestroyMap,
                EntityCommandBuffer = barrier.CreateCommandBuffer(),
                DestroyFromEntity = GetComponentDataFromEntity<Destroy>(true)
            }.Schedule(inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            inputDeps.Complete();

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
            if (m_SetDestroyMap.IsCreated)
            {
                m_SetDestroyMap.Dispose();
            }
        }
    }
}