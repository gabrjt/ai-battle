using Game.Components;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [UpdateInGroup(typeof(LogicGroup))]
    public class DebugKilledSystem : JobComponentSystem, IDisposable
    {
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Killed>.Concurrent KilledQueue;
            [ReadOnly] public ArchetypeChunkEntityType EntityType;
            [ReadOnly] public Random Random;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);

                var killedList = new NativeList<Entity>(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var killer = entityArray[Random.NextInt(0, entityArray.Length)];
                    var killed = entityArray[Random.NextInt(0, entityArray.Length)];

                    if (killer == killed || killedList.Contains(killed)) continue;

                    killedList.Add(killed);

                    KilledQueue.Enqueue(new Killed
                    {
                        This = killer,
                        Other = killed
                    });
                }

                killedList.Dispose();
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Killed> KilledQueue;
            public EntityCommandBuffer CommandBuffer;
            [ReadOnly] public EntityArchetype Archetype;

            public void Execute()
            {
                while (KilledQueue.TryDequeue(out var killedComponent))
                {
                    var killed = CommandBuffer.CreateEntity(Archetype);
                    CommandBuffer.SetComponent(killed, killedComponent);
                }
            }
        }

        private ComponentGroup m_Group;
        private NativeQueue<Killed> m_KilledQueue;
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
            m_KilledQueue = new NativeQueue<Killed>(Allocator.Persistent);
            m_Random = new Random((uint)Environment.TickCount);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventCommandBufferSystem = World.GetExistingManager<EventCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                KilledQueue = m_KilledQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                Random = m_Random
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                KilledQueue = m_KilledQueue,
                CommandBuffer = eventCommandBufferSystem.CreateCommandBuffer(),
                Archetype = m_Archetype
            }.Schedule(inputDeps);

            eventCommandBufferSystem.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();
            Dispose();
        }

        public void Dispose()
        {
            if (m_KilledQueue.IsCreated)
            {
                m_KilledQueue.Dispose();
            }
        }
    }
}