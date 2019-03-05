using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class SetDestroySystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Destroy>.Concurrent SetMap;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public ArchetypeChunkComponentType<Died> DiedType;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var diedArray = chunk.GetNativeArray(DiedType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = diedArray[entityIndex].This;

                    SetMap.TryAdd(entity, new Destroy());
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly] public NativeHashMap<Entity, Destroy> SetMap;
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                var entityArray = SetMap.GetKeyArray(Allocator.Temp);

                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];

                    CommandBuffer.AddComponent(entity, SetMap[entity]);
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;
        private EntityArchetype m_Archetype;
        private NativeHashMap<Entity, Destroy> m_SetMap;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Died>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Destroyed>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetMap = new NativeHashMap<Entity, Destroy>(m_Group.CalculateLength(), Allocator.TempJob);

            var setCommandBufferSystem = World.GetExistingManager<SetCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                SetMap = m_SetMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                DiedType = GetArchetypeChunkComponentType<Died>(true)
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetMap = m_SetMap,
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer(),
            }.Schedule(inputDeps);

            setCommandBufferSystem.AddJobHandleForProducer(inputDeps);

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
            if (m_SetMap.IsCreated)
            {
                m_SetMap.Dispose();
            }
        }
    }
}