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
            public NativeHashMap<Entity, Destination>.Concurrent SetMap;

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
            public ComponentDataFromEntity<Translation> PositionFromEntity;

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

                        SetMap.TryAdd(entity, new Destination
                        {
                            Value = destinationFound.Value,
                            LastValue = destinationFound.Value
                        });
                    }
                }
            }

            private void Stop(Entity entity)
            {
                var translation = PositionFromEntity[entity].Value;

                SetMap.TryAdd(entity, new Destination
                {
                    Value = translation,
                    LastValue = translation
                });
            }

            private void SetTargetDestination(Entity entity, Entity target)
            {
                if (DestinationFromEntity.Exists(entity))
                {
                    var lastDestination = DestinationFromEntity[entity].Value;
                    SetMap.TryAdd(entity, new Destination
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
            public NativeHashMap<Entity, Destination> SetMap;

            public EntityCommandBuffer CommandBuffer;

            public EntityCommandBuffer EventCommandBufferSystemSystem;

            [ReadOnly]
            public EntityArchetype Archetype;

            public ComponentDataFromEntity<Destination> DestinationFromEntity;

            public void Execute()
            {
                var entityArray = SetMap.GetKeyArray(Allocator.Temp);

                for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var destination = SetMap[entity];

                    if (DestinationFromEntity.Exists(entity))
                    {
                        CommandBuffer.SetComponent(entity, destination);

                        if (destination.Equals(DestinationFromEntity[entity]))
                        {
                            var destinationReached = EventCommandBufferSystemSystem.CreateEntity(Archetype);
                            EventCommandBufferSystemSystem.SetComponent(destinationReached, new DestinationReached { This = entity });
                        }
                    }
                    else
                    {
                        CommandBuffer.AddComponent(entity, destination);
                    }
                }

                entityArray.Dispose();
            }
        }

        private ComponentGroup m_Group;

        private NativeHashMap<Entity, Destination> m_SetMap;

        private EntityArchetype m_Archetype;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Translation>() },
                None = new[] { ComponentType.ReadWrite<Destination>(), ComponentType.ReadOnly<Dead>() }
            }, new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Event>() },
                Any = new[] { ComponentType.ReadOnly<DestinationFound>(), ComponentType.ReadOnly<TargetFound>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<DestinationReached>());
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            Dispose();

            m_SetMap = new NativeHashMap<Entity, Destination>(m_Group.CalculateLength(), Allocator.TempJob);

            var setSystem = World.GetExistingManager<SetCommandBufferSystem>();
            var eventSystem = World.GetExistingManager<EventCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                SetMap = m_SetMap.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                TargetFoundType = GetArchetypeChunkComponentType<TargetFound>(true),
                DestinationFoundType = GetArchetypeChunkComponentType<DestinationFound>(true),
                DeadFromEntity = GetComponentDataFromEntity<Dead>(true),
                PositionFromEntity = GetComponentDataFromEntity<Translation>(true),
                DestinationFromEntity = GetComponentDataFromEntity<Destination>()
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyJob
            {
                SetMap = m_SetMap,
                CommandBuffer = setSystem.CreateCommandBuffer(),
                EventCommandBufferSystemSystem = eventSystem.CreateCommandBuffer(),
                Archetype = m_Archetype,
                DestinationFromEntity = GetComponentDataFromEntity<Destination>()
            }.Schedule(inputDeps);

            setSystem.AddJobHandleForProducer(inputDeps);
            eventSystem.AddJobHandleForProducer(inputDeps);

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