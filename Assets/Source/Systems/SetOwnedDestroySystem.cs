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
        private struct ConsolidateDestroyedJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent DestroyedQueue;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Destroyed> DestroyedType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(DestroyedType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);
                    var destroyedArray = chunk.GetNativeArray(DestroyedType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        DestroyedQueue.Enqueue(destroyedArray[entityIndex].This);
                    }
                }
            }
        }

        [BurstCompile]
        private struct ConsolidateOwnedJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent OwnedQueue;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Owner> OwnerType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(OwnerType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);
                    var ownerArray = chunk.GetNativeArray(OwnerType);

                    for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        OwnedQueue.Enqueue(ownerArray[entityIndex].Value);
                    }
                }
            }
        }

        private struct AddDestroyedJob : IJob
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> OwnerArray;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> OwnedArray;

            [ReadOnly]
            public ComponentDataFromEntity<Owner> OwnerFromEntity;

            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                for (var entityIndex = 0; entityIndex < OwnedArray.Length; entityIndex++)
                {
                    var owner = OwnedArray[entityIndex];
                    var owned = OwnedArray[entityIndex];

                    if (!OwnerFromEntity.Exists(owned) || owner != OwnerFromEntity[owned].Value) continue;

                    EntityCommandBuffer.AddComponent(owned, new Destroy());
                }
            }
        }

        private ComponentGroup m_Group;

        private NativeQueue<Entity> m_DestroyedQueue;

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

            m_DestroyedQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_OwnedQueue = new NativeQueue<Entity>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_DestroyedQueue.Clear();

            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateDestroyedJob
            {
                DestroyedQueue = m_DestroyedQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                DestroyedType = GetArchetypeChunkComponentType<Destroyed>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ConsolidateOwnedJob
            {
                OwnedQueue = m_OwnedQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                OwnerType = GetArchetypeChunkComponentType<Owner>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            var entityIndex = 0;

            var destroyedArray = new NativeArray<Entity>(m_DestroyedQueue.Count, Allocator.TempJob);
            while (m_DestroyedQueue.TryDequeue(out var entity))
            {
                destroyedArray[entityIndex++] = entity;
            }

            entityIndex = 0;

            var ownedArray = new NativeArray<Entity>(m_OwnedQueue.Count, Allocator.TempJob);
            while (m_OwnedQueue.TryDequeue(out var entity))
            {
                ownedArray[entityIndex++] = entity;
            }

            inputDeps = new AddDestroyedJob
            {
                OwnerArray = destroyedArray,
                OwnedArray = ownedArray,
                EntityCommandBuffer = barrier.CreateCommandBuffer(),
                OwnerFromEntity = GetComponentDataFromEntity<Owner>(true)
            }.Schedule(inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_DestroyedQueue.IsCreated)
            {
                m_DestroyedQueue.Dispose();
            }

            if (m_OwnedQueue.IsCreated)
            {
                m_OwnedQueue.Dispose();
            }
        }
    }
}