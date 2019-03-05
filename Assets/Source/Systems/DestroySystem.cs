using Game.Components;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Systems
{
    [UpdateInGroup(typeof(DestroyGroup))]
    public class DestroySystem : JobComponentSystem
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Entity>.Concurrent PureEntityQueue;
            public NativeQueue<Entity>.Concurrent HealthBarQueue;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<HealthBar> HealthBarType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                if (chunk.Has(HealthBarType))
                {
                    for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        HealthBarQueue.Enqueue(entityArray[entityIndex]);
                    }
                }
                else
                {
                    for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                    {
                        PureEntityQueue.Enqueue(entityArray[entityIndex]);
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Entity> PureEntityQueue;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                while (PureEntityQueue.TryDequeue(out var entity))
                {
                    CommandBuffer.DestroyEntity(entity);
                }
            }
        }

        private ComponentGroup m_Group;
        private NativeQueue<Entity> m_PureEntityQueue;
        private NativeQueue<Entity> m_HealthBarQueue;
        internal Queue<GameObject> m_HealthBarPool;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>() },
                None = new[] { ComponentType.ReadOnly<HealthBar>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<HealthBar>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Components.Event>() }
            });

            m_PureEntityQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_HealthBarQueue = new NativeQueue<Entity>(Allocator.Persistent);
            m_HealthBarPool = new Queue<GameObject>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var destroyCommandBufferSystem = World.Active.GetExistingManager<DestroyCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                PureEntityQueue = m_PureEntityQueue.ToConcurrent(),
                HealthBarQueue = m_HealthBarQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                HealthBarType = GetArchetypeChunkComponentType<HealthBar>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                PureEntityQueue = m_PureEntityQueue,
                CommandBuffer = destroyCommandBufferSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps.Complete();

            while (m_HealthBarQueue.TryDequeue(out var entity))
            {
                var gameObject = EntityManager.GetComponentObject<RectTransform>(entity).parent.gameObject;
                gameObject.SetActive(false);
                m_HealthBarPool.Enqueue(gameObject);
            }

            var spawnAICharacterSystem = World.GetExistingManager<InstantiateAICharacterSystem>();
            var maxPoolCount = math.max(spawnAICharacterSystem.m_LastTotalCount, spawnAICharacterSystem.m_TotalCount);

            while (m_HealthBarPool.Count > maxPoolCount)
            {
                Object.Destroy(m_HealthBarPool.Dequeue());
            }

            destroyCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();
            Dispose();
        }

        public void Dispose()
        {
            if (m_PureEntityQueue.IsCreated)
            {
                m_PureEntityQueue.Dispose();
            }

            if (m_HealthBarQueue.IsCreated)
            {
                m_HealthBarQueue.Dispose();
            }

            m_HealthBarPool.Clear();
        }
    }
}