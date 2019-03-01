using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;

namespace Game.Systems
{
    public class SetDestinationSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, Destination>.Concurrent SetDestinationMap;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Target> TargetType;

            [ReadOnly]
            public ArchetypeChunkComponentType<TargetFound> TargetFoundType;

            [ReadOnly]
            public ArchetypeChunkComponentType<DestinationFound> DestinationFoundType;

            [ReadOnly]
            public ComponentDataFromEntity<Dead> DeadFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Destination> DestinationFromEntity;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                if (chunk.Has(TargetType))
                {
                    var entityArray = chunk.GetNativeArray(EntityType);
                    var targetArray = chunk.GetNativeArray(TargetType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var entity = entityArray[entityIndex];
                        var target = targetArray[entityIndex];

                        if (DeadFromEntity.Exists(target.Value)) continue;

                        SetTargetDestination(entity, target.Value);
                    }
                }
                if (chunk.Has(TargetFoundType))
                {
                    var targetFoundArray = chunk.GetNativeArray(TargetFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var targetFound = targetFoundArray[entityIndex];

                        var entity = targetFound.This;

                        if (DeadFromEntity.Exists(targetFound.Other))
                        {
                            Stop(entity);
                        }
                        else
                        {
                            SetTargetDestination(entity, targetFound.Other);
                        }
                    }
                }
                else if (chunk.Has(DestinationFoundType))
                {
                    var destinationFoundArray = chunk.GetNativeArray(DestinationFoundType);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var destinationFound = destinationFoundArray[entityIndex];

                        var entity = destinationFound.This;

                        SetDestinationMap.TryAdd(entity, new Destination
                        {
                            Value = destinationFound.Value,
                            LastValue = destinationFound.Value
                        });
                    }
                }
            }

            private void Stop(Entity entity)
            {
                if (!PositionFromEntity.Exists(entity)) return;

                var position = PositionFromEntity[entity].Value;

                SetDestinationMap.TryAdd(entity, new Destination
                {
                    Value = position,
                    LastValue = position
                });
            }

            private void SetTargetDestination(Entity entity, Entity target)
            {
                if (DestinationFromEntity.Exists(entity) && PositionFromEntity.Exists(target))
                {
                    var lastDestination = DestinationFromEntity[entity].Value;
                    SetDestinationMap.TryAdd(entity, new Destination
                    {
                        Value = PositionFromEntity[target].Value,
                        LastValue = lastDestination
                    });
                }
                else
                {
                    Stop(entity);
                }
            }
        }

        private struct ApplyJob : IJob
        {
            [ReadOnly]
            public NativeHashMap<Entity, Destination> SetDestinationMap;

            [ReadOnly]
            public EntityCommandBuffer EntityCommandBuffer;

            [ReadOnly]
            public EntityCommandBuffer EventCommandBuffer;

            [ReadOnly]
            public EntityArchetype Archetype;

            public ComponentDataFromEntity<Destination> DestinationFromEntity;

            public void Execute()
            {
                var entityArray = SetDestinationMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var destination = SetDestinationMap[entity];

                    if (DestinationFromEntity.Exists(entity))
                    {
                        EntityCommandBuffer.SetComponent(entity, destination);

                        if (destination.Value.Equals(DestinationFromEntity[entity].Value))
                        {
                            var destinationReached = EventCommandBuffer.CreateEntity(Archetype);
                            EventCommandBuffer.SetComponent(destinationReached, new DestinationReached { This = entity });
                        }
                    }
                    else
                    {
                        EntityCommandBuffer.AddComponent(entity, destination);
                    }
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Destination> m_SetDestinationMap;

        private EntityArchetype m_Archetype;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Position>() },
                None = new[] { ComponentType.Create<Destination>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationFound>(), ComponentType.ReadOnly<TargetFound>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.Create<Event>(), ComponentType.Create<DestinationReached>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetDestinationMap = new NativeHashMap<Entity, Destination>(m_Group.CalculateLength(), Allocator.TempJob);

            var barrier = World.GetExistingManager<SetBarrier>();
            var eventBarrier = World.GetExistingManager<EventBarrier>();

            inputDeps = new ConsolidateJob
            {
                SetDestinationMap = m_SetDestinationMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                TargetFoundType = GetArchetypeChunkComponentType<TargetFound>(true),
                DestinationFoundType = GetArchetypeChunkComponentType<DestinationFound>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true),
                DestinationFromEntity = GetComponentDataFromEntity<Destination>()
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetDestinationMap = m_SetDestinationMap,
                EntityCommandBuffer = barrier.CreateCommandBuffer(),
                EventCommandBuffer = eventBarrier.CreateCommandBuffer(),
                Archetype = m_Archetype,
                DestinationFromEntity = GetComponentDataFromEntity<Destination>()
            }.Schedule(inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);
            eventBarrier.AddJobHandleForProducer(inputDeps);

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
            if (m_SetDestinationMap.IsCreated)
            {
                m_SetDestinationMap.Dispose();
            }
        }
    }
}