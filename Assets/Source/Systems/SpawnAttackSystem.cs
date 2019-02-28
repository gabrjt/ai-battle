using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    //[DisableAutoCreation]
    public class SpawnAttackSystem : JobComponentSystem
    {
        private struct ConsolidateJob : IJobChunk
        {
            public NativeHashMap<Entity, SpawnData>.Concurrent SpawnDataMap;

            [ReadOnly]
            public EntityArchetype AttackedArchetype;

            public EntityCommandBuffer.Concurrent EntityCommandBuffer;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Target> TargetType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Position> PositionType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Attack> AttackType;

            [ReadOnly]
            public ArchetypeChunkComponentType<AttackDistance> AttackDistanceType;

            [ReadOnly]
            public ArchetypeChunkComponentType<AttackDuration> AttackDurationType;

            [ReadOnly]
            public ArchetypeChunkComponentType<AttackSpeed> AttackSpeedType;

            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

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

                    var position = positionArray[entityIndex].Value;
                    var targetPosition = PositionFromEntity[target.Value].Value;
                    var attackDistance = attackDistanceArray[entityIndex];
                    var attackSpeed = attackSpeedArray[entityIndex].Value;

                    if (math.distance(position, targetPosition) > attackDistance.Max) continue;

                    var direction = math.normalizesafe(targetPosition - position);

                    var origin = position + new float3(0, 0.35f, 0);

                    SpawnDataMap.TryAdd(entity, new SpawnData
                    {
                        Owner = entity,
                        Position = new Position
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
                            Origin = position,
                            Value = attackDistance.Max * attackDistance.Max
                        },
                        Speed = new Speed { Value = attackSpeed * 5 },
                        Damage = new Damage { Value = attackArray[entityIndex].Value }
                    });

                    var duration = attackDurationArray[entityIndex].Value / attackSpeed;

                    EntityCommandBuffer.AddComponent(m_ThreadIndex, entity, new Attacking
                    {
                        Duration = duration,
                        StartTime = Time
                    });

                    EntityCommandBuffer.AddComponent(m_ThreadIndex, entity, new Cooldown
                    {
                        Duration = duration,
                        StartTime = Time
                    });

                    var attacked = EntityCommandBuffer.CreateEntity(m_ThreadIndex, AttackedArchetype);
                    EntityCommandBuffer.SetComponent(m_ThreadIndex, attacked, new Attacked { This = entity });
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
            public ComponentDataFromEntity<Position> PositionFromEntity;

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

                PositionFromEntity[entity] = spawnData.Position;
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

        private struct SpawnData
        {
            public Entity Owner;

            public Position Position;

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

        private NativeHashMap<Entity, SpawnData> m_SpawnDataMap;

        private MRandom m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[]
                {
                    ComponentType.ReadOnly<Target>(),
                    ComponentType.ReadOnly<Position>(),
                    ComponentType.ReadOnly<Attack>(),
                    ComponentType.ReadOnly<AttackDistance>(),
                    ComponentType.ReadOnly<AttackDuration>(),
                    ComponentType.ReadOnly<AttackSpeed>()
                },
                None = new[] { ComponentType.ReadOnly<Cooldown>(), ComponentType.ReadOnly<Attacking>(), ComponentType.ReadOnly<Dead>() }
            });

            m_Archetype = EntityManager.CreateArchetype(
                ComponentType.Create<AttackInstance>(),
                ComponentType.Create<Position>(),
                ComponentType.Create<Rotation>(),
                ComponentType.Create<Scale>(),
#if DEBUG_ATTACK
                ComponentType.Create<RenderMesh>(),
#endif
                ComponentType.Create<Speed>(),
                ComponentType.Create<Direction>(),
                ComponentType.Create<Velocity>(),
                ComponentType.Create<Damage>(),
                ComponentType.Create<MaxSqrDistance>(),
                ComponentType.Create<Prefab>());

            m_AttackedArchetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<Attacked>());

            m_Prefab = EntityManager.CreateEntity(m_Archetype);

#if DEBUG_ATTACK
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            var material = sphere.GetComponent<MeshRenderer>().sharedMaterial;
#endif

            EntityManager.SetComponentData(m_Prefab, new Scale { Value = new float3(0.25f, 0.25f, 0.25f) });

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

            m_SpawnDataMap = new NativeHashMap<Entity, SpawnData>(5000, Allocator.Persistent);

            m_Random = new MRandom((uint)System.Environment.TickCount);
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            m_SpawnDataMap.Clear();

            var barrier = World.GetExistingManager<EndFrameBarrier>();

            inputDeps = new ConsolidateJob
            {
                SpawnDataMap = m_SpawnDataMap.ToConcurrent(),
                EntityCommandBuffer = barrier.CreateCommandBuffer().ToConcurrent(),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                PositionType = GetArchetypeChunkComponentType<Position>(true),
                AttackType = GetArchetypeChunkComponentType<Attack>(true),
                AttackDistanceType = GetArchetypeChunkComponentType<AttackDistance>(true),
                AttackDurationType = GetArchetypeChunkComponentType<AttackDuration>(true),
                AttackSpeedType = GetArchetypeChunkComponentType<AttackSpeed>(true),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true),
                AttackedArchetype = m_AttackedArchetype,
                Time = Time.time
            }.Schedule(m_Group, inputDeps);

            inputDeps.Complete();

            var entityArray = new NativeArray<Entity>(m_SpawnDataMap.Length, Allocator.TempJob);

            EntityManager.Instantiate(m_Prefab, entityArray);

            inputDeps = new ApplyJob
            {
                EntityArray = entityArray,
                SpawnDataArray = m_SpawnDataMap.GetValueArray(Allocator.TempJob),
                AttackInstanceFromEntity = GetComponentDataFromEntity<AttackInstance>(),
                PositionFromEntity = GetComponentDataFromEntity<Position>(),
                RotationFromEntity = GetComponentDataFromEntity<Rotation>(),
                DirectionFromEntity = GetComponentDataFromEntity<Direction>(),
                MaxSqrDistanceFromEntity = GetComponentDataFromEntity<MaxSqrDistance>(),
                SpeedFromEntity = GetComponentDataFromEntity<Speed>(),
                DamageFromEntity = GetComponentDataFromEntity<Damage>(),
                VelocityFromEntity = GetComponentDataFromEntity<Velocity>(),
            }.Schedule(entityArray.Length, 64, inputDeps);

            barrier.AddJobHandleForProducer(inputDeps);

            return inputDeps;
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SpawnDataMap.IsCreated)
            {
                m_SpawnDataMap.Dispose();
            }
        }
    }
}