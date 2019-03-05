using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class RemoveIdleSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Idle>.Concurrent RemoveMap;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<Idle> IdleType;
            [ReadOnly] public ArchetypeChunkComponentType<IdleTimeExpired> IdleTimeExpiredType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(IdleType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        RemoveMap.TryAdd(entityArray[entityIndex], new Idle());
                    }
                }
                else if (chunk.Has(IdleTimeExpiredType))
                {
                    var idleTimeExpiredArray = chunk.GetNativeArray(IdleTimeExpiredType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        RemoveMap.TryAdd(idleTimeExpiredArray[entityIndex].This, new Idle());
                    }
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly] public NativeHashMap<Entity, Idle> RemoveMap;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var entityArray = RemoveMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    CommandBuffer.RemoveComponent<Idle>(entityArray[entityIndex]);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Idle> m_RemoveMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadWrite<Idle>() },
                Any = new[] { ComponentType.ReadOnly<Destination>(), ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<IdleTimeExpired>() }
            });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_RemoveMap = new NativeHashMap<Entity, Idle>(m_Group.CalculateLength(), Allocator.TempJob);

            var removeSystem = World.GetExistingManager<RemoveCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                RemoveMap = m_RemoveMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                IdleType = GetArchetypeChunkComponentType<Idle>(true),
                IdleTimeExpiredType = GetArchetypeChunkComponentType<IdleTimeExpired>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                RemoveMap = m_RemoveMap,
                CommandBuffer = removeSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            removeSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
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