using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class DebugKilledSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Killed>.Concurrent KilledMap;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public Random Random;
            [ReadOnly] public ComponentDataFromEntity<Dead> DeadFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var killer = entityArray[Random.NextInt(0, entityArray.Length)];
                    var killed = entityArray[Random.NextInt(0, entityArray.Length)];

                    if (DeadFromEntity.Exists(killer) || DeadFromEntity.Exists(killed)) continue;

                    KilledMap.TryAdd(killed, new Killed
                    {
                        This = killer,
                        Other = killed
                    });
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly] public NativeHashMap<Entity, Killed> KilledMap;
            public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public EntityArchetype Archetype;

            public void Execute()
            {
                var entityArray = KilledMap.GetKeyArray(Allocator.Temp);
                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var killed = CommandBuffer.CreateEntity(Archetype);
                    CommandBuffer.SetComponent(killed, KilledMap[entityArray[entityIndex]]);
                }
            }
        }

        private ComponentGroup m_Group;
        private NativeHashMap<Entity, Killed> m_KilledMap;
        private EntityArchetype m_Archetype;
        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadOnly<Dead>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Killed>());
            m_Random = new Random((uint)Environment.TickCount);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_KilledMap = new NativeHashMap<Entity, Killed>(m_Group.CalculateLength(), Allocator.TempJob);
            var eventCommandBufferSystem = World.GetExistingManager<EventCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                KilledMap = m_KilledMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                Random = m_Random
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                KilledMap = m_KilledMap,
                CommandBuffer = eventCommandBufferSystem.CreateCommandBuffer(),
                Archetype = m_Archetype
            }.Schedule(inputDeps);

            eventCommandBufferSystem.AddJobHandleForProducer(inputDeps);

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
            if (m_KilledMap.IsCreated)
            {
                m_KilledMap.Dispose();
            }
        }
    }
}