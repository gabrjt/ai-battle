﻿using Game.Components;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class SpawnAttackSystem : JobComponentSystem, IDisposable
    {
        [BurstCompile]
        private struct ConsolidateJob : IJobChunk
        {
            public NativeQueue<SpawnData>.Concurrent SpawnDataQueue;

            public NativeQueue<ApplyAttackingData>.Concurrent ApplyAttackingQueue;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Target> TargetType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Translation> PositionType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Attack> AttackType;

            [ReadOnly]
            public ArchetypeChunkComponentType<AttackDistance> AttackDistanceType;

            [ReadOnly]
            public ArchetypeChunkComponentType<AttackDuration> AttackDurationType;

            [ReadOnly]
            public ArchetypeChunkComponentType<AttackSpeed> AttackSpeedType;

            [ReadOnly]
            public ComponentDataFromEntity<Translation> PositionFromEntity;

            public float Time;

            [NativeSetThreadIndex]
            private readonly int m_ThreadIndex;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var entityArray = chunk.GetNativeArray(EntityType);
                var targetArray = chunk.GetNativeArray(TargetType);
                var positionArray = chunk.GetNativeArray(PositionType);
                var attackArray = chunk.GetNativeArray(AttackType);
                var attackDistanceArray = chunk.GetNativeArray(AttackDistanceType);
                var attackDurationArray = chunk.GetNativeArray(AttackDurationType);
                var attackSpeedArray = chunk.GetNativeArray(AttackSpeedType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var target = targetArray[entityIndex];

                    if (!PositionFromEntity.Exists(target.Value)) continue;

                    var translation = positionArray[entityIndex].Value;
                    var targetPosition = PositionFromEntity[target.Value].Value;
                    var attackDistance = attackDistanceArray[entityIndex];
                    var attackSpeed = attackSpeedArray[entityIndex].Value;

                    if (math.distance(translation, targetPosition) > attackDistance.Max) continue;

                    var direction = math.normalizesafe(targetPosition - translation);

                    var origin = translation + new float3(0, 0.35f, 0);

                    SpawnDataQueue.Enqueue(new SpawnData
                    {
                        Owner = entity,
                        Translation = new Translation
                        {
                            Value = origin
                        },
                        Rotation = new Rotation { Value = quaternion.LookRotation(direction, math.up()) },
                        Direction = new Direction
                        {
                            Value = direction
                        },
                        MaxSqrDistance = new MaxSqrDistance
                        {
                            Origin = translation,
                            Value = attackDistance.Max * attackDistance.Max
                        },
                        Speed = new Speed { Value = attackSpeed * 5 },
                        Damage = new Damage { Value = attackArray[entityIndex].Value }
                    });

                    var duration = attackDurationArray[entityIndex].Value / attackSpeed;

                    ApplyAttackingQueue.Enqueue(new ApplyAttackingData
                    {
                        Entity = entity,
                        Attacking = new Attacking
                        {
                            Duration = duration,
                            StartTime = Time
                        },
                        Cooldown = new Cooldown
                        {
                            Duration = duration,
                            StartTime = Time
                        }
                    });
                }
            }
        }

        private struct ApplyAttackingJob : IJob
        {
            public NativeQueue<ApplyAttackingData> ApplyAttackingQueue;

            [ReadOnly]
            public EntityArchetype AttackedArchetype;

            [ReadOnly]
            public EntityCommandBuffer CommandBuffer;

            [ReadOnly]
            public EntityCommandBuffer EventCommandBufferSystemSystem;

            public void Execute()
            {
                while (ApplyAttackingQueue.TryDequeue(out var applyAttackingData))
                {
                    var entity = applyAttackingData.Entity;

                    CommandBuffer.AddComponent(entity, applyAttackingData.Attacking);
                    CommandBuffer.AddComponent(entity, applyAttackingData.Cooldown);

                    var attacked = EventCommandBufferSystemSystem.CreateEntity(AttackedArchetype);
                    EventCommandBufferSystemSystem.SetComponent(attacked, new Attacked { This = entity });
                }
            }
        }

        [BurstCompile]
        private struct ApplyJob : IJobParallelFor
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<SpawnData> SpawnDataArray;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<AttackInstance> AttackInstanceFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Translation> PositionFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Rotation> RotationFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Direction> DirectionFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<MaxSqrDistance> MaxSqrDistanceFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Speed> SpeedFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Damage> DamageFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Velocity> VelocityFromEntity;

            public void Execute(int index)
            {
                var spawnData = SpawnDataArray[index];
                var entity = EntityArray[index];

                AttackInstanceFromEntity[entity] = new AttackInstance
                {
                    Owner = spawnData.Owner,
                    Radius = 0.25f
                };

                PositionFromEntity[entity] = spawnData.Translation;
                RotationFromEntity[entity] = spawnData.Rotation;
                DirectionFromEntity[entity] = spawnData.Direction;
                MaxSqrDistanceFromEntity[entity] = spawnData.MaxSqrDistance;
                SpeedFromEntity[entity] = spawnData.Speed;
                DamageFromEntity[entity] = spawnData.Damage;

                VelocityFromEntity[entity] = new Velocity
                {
                    Value = spawnData.Direction.Value * spawnData.Speed.Value
                };
            }
        }

        private struct ApplyAttackingData
        {
            public Entity Entity;

            public Attacking Attacking;

            public Cooldown Cooldown;
        }

        private struct SpawnData
        {
            public Entity Owner;

            public Translation Translation;

            public Rotation Rotation;

            public Direction Direction;

            public MaxSqrDistance MaxSqrDistance;

            public Speed Speed;

            public Damage Damage;
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private EntityArchetype m_AttackedArchetype;

        private Entity m_Prefab;

        private NativeQueue<SpawnData> m_SpawnDataQueue;

        private NativeQueue<ApplyAttackingData> m_ApplyAttackingQueue;

        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Target>(),
                    ComponentType.ReadOnly<Translation>(),
                    ComponentType.ReadOnly<Attack>(),
                    ComponentType.ReadOnly<AttackDistance>(),
                    ComponentType.ReadOnly<AttackDuration>(),
                    ComponentType.ReadOnly<AttackSpeed>()
                },
                None = new[] { ComponentType.ReadOnly<Cooldown>(), ComponentType.ReadOnly<Attacking>(), ComponentType.ReadOnly<Dead>() }
            });

            m_Archetype = EntityManager.CreateArchetype(
                ComponentType.ReadWrite<AttackInstance>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<Scale>(),
#if DEBUG_ATTACK
                ComponentType.ReadWrite<RenderMesh>(),
#endif
                ComponentType.ReadWrite<Speed>(),
                ComponentType.ReadWrite<Direction>(),
                ComponentType.ReadWrite<Velocity>(),
                ComponentType.ReadWrite<Damage>(),
                ComponentType.ReadWrite<MaxSqrDistance>(),
                ComponentType.ReadWrite<Prefab>());

            m_AttackedArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<Attacked>());

            m_Prefab = EntityManager.CreateEntity(m_Archetype);

#if DEBUG_ATTACK
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            var material = sphere.GetComponent<MeshRenderer>().sharedMaterial;
#endif

            EntityManager.SetComponentData(m_Prefab, new Scale { Value = 0.25f });

#if DEBUG_ATTACK
            EntityManager.SetSharedComponentData(m_Prefab, new RenderMesh
            {
                mesh = mesh,
                material = material,
                subMesh = 0,
                layer = 0,
                castShadows = UnityEngine.Rendering.ShadowCastingMode.On,
                receiveShadows = true
            });

            Object.Destroy(sphere);
#endif

            m_Random = new Random((uint)Environment.TickCount);

            m_ApplyAttackingQueue = new NativeQueue<ApplyAttackingData>(Allocator.Persistent);
            m_SpawnDataQueue = new NativeQueue<SpawnData>(Allocator.Persistent);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            var setSystem = World.GetExistingManager<SetCommandBufferSystem>();
            var eventSystem = World.GetExistingManager<EventCommandBufferSystem>();

            inputDeps = new ConsolidateJob
            {
                SpawnDataQueue = m_SpawnDataQueue.ToConcurrent(),
                ApplyAttackingQueue = m_ApplyAttackingQueue.ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                PositionType = GetArchetypeChunkComponentType<Translation>(true),
                AttackType = GetArchetypeChunkComponentType<Attack>(true),
                AttackDistanceType = GetArchetypeChunkComponentType<AttackDistance>(true),
                AttackDurationType = GetArchetypeChunkComponentType<AttackDuration>(true),
                AttackSpeedType = GetArchetypeChunkComponentType<AttackSpeed>(true),
                PositionFromEntity = GetComponentDataFromEntity<Translation>(true),
                Time = Time.time
            }.Schedule(m_Group, inputDeps);

            inputDeps = new ApplyAttackingJob
            {
                ApplyAttackingQueue = m_ApplyAttackingQueue,
                AttackedArchetype = m_AttackedArchetype,
                CommandBuffer = setSystem.CreateCommandBuffer(),
                EventCommandBufferSystemSystem = eventSystem.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps.Complete();

            var entityArray = new NativeArray<Entity>(m_SpawnDataQueue.Count, Allocator.TempJob);
            var spawnDataArray = new NativeArray<SpawnData>(m_SpawnDataQueue.Count, Allocator.TempJob);

            var count = 0;
            while (m_SpawnDataQueue.TryDequeue(out var spawnData))
            {
                spawnDataArray[count++] = spawnData;
            }

            EntityManager.Instantiate(m_Prefab, entityArray);

            inputDeps = new ApplyJob
            {
                EntityArray = entityArray,
                SpawnDataArray = spawnDataArray,
                AttackInstanceFromEntity = GetComponentDataFromEntity<AttackInstance>(),
                PositionFromEntity = GetComponentDataFromEntity<Translation>(),
                RotationFromEntity = GetComponentDataFromEntity<Rotation>(),
                DirectionFromEntity = GetComponentDataFromEntity<Direction>(),
                MaxSqrDistanceFromEntity = GetComponentDataFromEntity<MaxSqrDistance>(),
                SpeedFromEntity = GetComponentDataFromEntity<Speed>(),
                DamageFromEntity = GetComponentDataFromEntity<Damage>(),
                VelocityFromEntity = GetComponentDataFromEntity<Velocity>(),
            }.Schedule(entityArray.Length, 64, inputDeps);

            setSystem.AddJobHandleForProducer(inputDeps);
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
            if (m_SpawnDataQueue.IsCreated)
            {
                m_SpawnDataQueue.Dispose();
            }

            if (m_ApplyAttackingQueue.IsCreated)
            {
                m_ApplyAttackingQueue.Dispose();
            }
        }
    }
}