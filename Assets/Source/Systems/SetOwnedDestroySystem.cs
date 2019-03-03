using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class SetOwnedDestroySystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Entity>.Concurrent OwnerMap;

            public NativeQueue<Entity>.Concurrent OwnedQueue;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Owner> OwnerType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Destroyed> DestroyedType;

            [ReadOnly]
            public ComponentDataFromEntity<Destroy> DestroyFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(DestroyedType))
                {
                    var destroyedArray = chunk.GetNativeArray(DestroyedType);

                    for (int entityIndex = 0; entityIndex < destroyedArray.Length; entityIndex++)
                    {
                        var entity = destroyedArray[entityIndex].This;

                        OwnerMap.TryAdd(entity, default);
                    }
                }
                else if (chunk.Has(OwnerType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);
                    var ownerArray = chunk.GetNativeArray(OwnerType);
                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var owner = ownerArray[entityIndex].Value;

                        if (!DestroyFromEntity.Exists(owner)) continue;

                        OwnerMap.TryAdd(owner, entity);
                        OwnedQueue.Enqueue(entity);
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeHashMap<Entity, Entity> OwnerMap;

            public NativeQueue<Entity> OwnedQueue;

            [ReadOnly]
            public ComponentDataFromEntity<Owner> OwnerFromEntity;

            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var ownerArray = OwnerMap.GetKeyArray(Allocator.Temp);

                var entityIndex = 0;
                var ownedArray = new NativeArray<Entity>(OwnedQueue.Count, Allocator.Temp);
                while (OwnedQueue.TryDequeue(out var entity))
                {
                    ownedArray[entityIndex++] = entity;
                }

                for (int ownerIndex = 0; ownerIndex < ownerArray.Length; ownerIndex++)
                {
                    for (var ownedIndex = 0; ownedIndex < ownedArray.Length; ownedIndex++)
                    {
                        var owner = ownerArray[ownerIndex];
                        var owned = ownedArray[ownedIndex];

                        if (owner != OwnerFromEntity[owned].Value) continue;

                        CommandBuffer.AddComponent(owned, new Destroy());
                        CommandBuffer.AddComponent(owned, new Disabled());
                    }
                }

                ownerArray.Dispose();
                ownedArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Entity> m_OwnerMap;

        private NativeQueue<Entity> m_OwnedQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Destroyed>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Owner>() },
                None = new[] { ComponentType.Create<Destroy>() }
            });

            m_OwnedQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            DisposeMap();

            m_OwnerMap = new NativeHashMap<Entity, Entity>(m_Group.CalculateLength(), Allocator.TempJob);

            var destroyBarrier = World.GetExistingManager<DestroyBarrier>();

            inputDeps = new ConsolidateJob
            {
                OwnerMap = m_OwnerMap.ToConcurrent(),
                OwnedQueue = m_OwnedQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                OwnerType = GetArchetypeChunkComponentType<Owner>(true),
                DestroyedType = GetArchetypeChunkComponentType<Destroyed>(true),
                DestroyFromEntity = GetComponentDataFromEntity<Destroy>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                OwnerMap = m_OwnerMap,
                OwnedQueue = m_OwnedQueue,
                CommandBuffer = destroyBarrier.CreateCommandBuffer(),
                OwnerFromEntity = GetComponentDataFromEntity<Owner>(true)
            }.Schedule(inputDeps);

            destroyBarrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();

            DisposeMap();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            Dispose();
        }

        private void DisposeMap()
        {
            if (m_OwnerMap.IsCreated)
            {
                m_OwnerMap.Dispose();
            }
        }

        public void Dispose()
        {
            DisposeMap();

            if (m_OwnedQueue.IsCreated)
            {
                m_OwnedQueue.Dispose();
            }
        }
    }
}