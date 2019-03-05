using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Systems
{
    public class DamageSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<Killed>.Concurrent KilledQueue;

            [ReadOnly]
            public ArchetypeChunkComponentType<Damaged> DamagedType;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Health> HealthFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var damagedArray = chunk.GetNativeArray(DamagedType);

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var damaged = damagedArray[entityIndex];
                    var damageSource = damaged.This;
                    var damageTarget = damaged.Other;
                    var damage = damaged.Value;

                    var targetHealth = HealthFromEntity[damageTarget];

                    targetHealth = new Health { Value = targetHealth.Value - damage };

                    if (targetHealth.Value <= 0)
                    {
                        KilledQueue.Enqueue(new Killed
                        {
                            This = damageSource,
                            Other = damageTarget
                        });
                    }

                    HealthFromEntity[damageTarget] = targetHealth;
                }
            }
        }

        private struct ApplyJob : IJob
        {
            public NativeQueue<Killed> KilledQueue;

            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public EntityArchetype Archetype;

            public void Execute()
            {
                while (KilledQueue.TryDequeue(out var killedComponent))
                {
                    var entity = CommandBuffer.CreateEntity(Archetype);
                    CommandBuffer.SetComponent(entity, killedComponent);
                }
            }
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private NativeQueue<Killed> m_KilledQueue;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<Damaged>() },
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Killed>());

            m_KilledQueue = new NativeQueue<Killed>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var eventSystem = World.GetExistingManager<EventCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                KilledQueue = m_KilledQueue.ToConcurrent(),
                DamagedType = GetArchetypeChunkComponentType<Damaged>(true),
                HealthFromEntity = GetComponentDataFromEntity<Health>()
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                KilledQueue = m_KilledQueue,
                CommandBuffer = eventSystem.CreateCommandBuffer(),
                Archetype = m_Archetype
            }.Schedule(inputDeps);

            eventSystem.AddJobHandleForProducer(inputDeps);

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