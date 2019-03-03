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
            public NativeHashMap<Entity, Destroy>.Concurrent SetMap;

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

                        SetMap.TryAdd(entity, default);
                    }
                }
                else if (chunk.Has(MaxDistanceReachedType))
                {
                    var maxDistanceReachedArray = chunk.GetNativeArray(MaxDistanceReachedType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = maxDistanceReachedArray[entityIndex].This;

                        SetMap.TryAdd(entity, default);
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Destroy> SetMap;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var entityArray = SetMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    CommandBuffer.AddComponent(entity, SetMap[entity]);
                    CommandBuffer.AddComponent(entity, new Disabled());
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeQueue<Entity> m_SetQueue;

        private NativeHashMap<Entity, Destroy> m_SetMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<Collided>(), ComponentType.ReadOnly<MaxDistanceReached>() },
                None = new[] { ComponentType.Create<Destroy>() }
            });

            m_SetQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetMap = new NativeHashMap<Entity, Destroy>(m_Group.CalculateLength(), Allocator.TempJob);

            var destroyBarrier = World.GetExistingManager<DestroyBarrier>();

            inputDeps = new ConsolidateJob
            {
                SetMap = m_SetMap.ToConcurrent(),
                CollidedType = GetArchetypeChunkComponentType<Collided>(true),
                MaxDistanceReachedType = GetArchetypeChunkComponentType<MaxDistanceReached>(true),
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetMap = m_SetMap,
                CommandBuffer = destroyBarrier.CreateCommandBuffer()
            }.Schedule(inputDeps);

            destroyBarrier.AddJobHandleForProducer(inputDeps);

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
            if (m_SetQueue.IsCreated)
            {
                m_SetQueue.Dispose();
            }

            if (m_SetMap.IsCreated)
            {
                m_SetMap.Dispose();
            }
        }
    }
}