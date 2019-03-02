using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class SetOwnedDestroySystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent OwnerQueue;

            public NativeQueue<Entity>.Concurrent OwnedQueue;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Owner> OwnerType;

            [ReadOnly]
            public ComponentDataFromEntity<Destroyed> DestroyedFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var isOwner = chunk.Has(OwnerType);

                var entityArray = chunk.GetNativeArray(EntityType);

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    if (DestroyedFromEntity.Exists(entity))
                    {
                        OwnerQueue.Enqueue(DestroyedFromEntity[entity].This);
                    }
                    else if (isOwner)
                    {
                        OwnedQueue.Enqueue(entity);
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Entity> OwnerQueue;

            public NativeQueue<Entity> OwnedQueue;

            [ReadOnly]
            public ComponentDataFromEntity<Owner> OwnerFromEntity;

            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                var entityIndex = 0;

                var ownerArray = new NativeArray<Entity>(OwnerQueue.Count, Allocator.Temp);
                while (OwnerQueue.TryDequeue(out var entity))
                {
                    ownerArray[entityIndex++] = entity;
                }

                entityIndex = 0;

                var ownedArray = new NativeArray<Entity>(OwnedQueue.Count, Allocator.Temp);
                while (OwnedQueue.TryDequeue(out var entity))
                {
                    ownedArray[entityIndex++] = entity;
                }

                for (var ownedIndex = 0; ownedIndex < ownedArray.Length; ownedIndex++)
                {
                    for (int ownerIndex = 0; ownerIndex < ownerArray.Length; ownerIndex++)
                    {
                        var owner = ownerArray[ownerIndex];
                        var owned = ownedArray[ownedIndex];

                        if (!OwnerFromEntity.Exists(owned) || owner != OwnerFromEntity[owned].Value) continue;

                        EntityCommandBuffer.AddComponent(owned, new Destroy());
                        EntityCommandBuffer.AddComponent(owned, new Disabled());
                    }
                }

                ownerArray.Dispose();
                ownedArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeQueue<Entity> m_OwnerQueue;

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

            m_OwnerQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_OwnedQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setBarrier = World.GetExistingManager<SetBarrier>();

            inputDeps = new ConsolidateJob
            {
                OwnerQueue = m_OwnerQueue.ToConcurrent(),
                OwnedQueue = m_OwnedQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                OwnerType = GetArchetypeChunkComponentType<Owner>(true),
                DestroyedFromEntity = GetComponentDataFromEntity<Destroyed>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                OwnerQueue = m_OwnerQueue,
                OwnedQueue = m_OwnedQueue,
                EntityCommandBuffer = setBarrier.CreateCommandBuffer(),
                OwnerFromEntity = GetComponentDataFromEntity<Owner>(true)
            }.Schedule(inputDeps);

            setBarrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_OwnerQueue.IsCreated)
            {
                m_OwnerQueue.Dispose();
            }

            if (m_OwnedQueue.IsCreated)
            {
                m_OwnedQueue.Dispose();
            }
        }
    }
}