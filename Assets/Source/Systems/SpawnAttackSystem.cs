using Game.Components;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    //[DisableAutoCreation]
    public class SpawnAttackSystem : ComponentSystem
    {
        private struct ConsolidateJob : IJob
        {
            public float Time;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<ArchetypeChunk> ChunkArray;

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

            public NativeList<AttackSpawnData> EntitySpawnList;

            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            [ReadOnly]
            public EntityArchetype AttackedArchetype;

            public EntityCommandBuffer EntityCommandBuffer;

            public void Execute()
            {
                var firstEntityIndex = 0;

                for (var chunkIndex = 0; chunkIndex < ChunkArray.Length; chunkIndex++)
                {
                    var chunk = ChunkArray[chunkIndex];

                    Execute(chunk, chunkIndex, firstEntityIndex);

                    firstEntityIndex += chunk.Count;
                }
            }

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

                    EntitySpawnList.Add(new AttackSpawnData
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

                    EntityCommandBuffer.AddComponent(entity, new Attacking
                    {
                        Duration = duration,
                        StartTime = Time
                    });

                    EntityCommandBuffer.AddComponent(entity, new Cooldown
                    {
                        Duration = duration,
                        StartTime = Time
                    });

                    var attacked = EntityCommandBuffer.CreateEntity(AttackedArchetype);
                    EntityCommandBuffer.SetComponent(attacked, new Attacked { This = entity });
                }
            }
        }

        [BurstCompile]
        private struct ApplyJob : IJobParallelFor
        {
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

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;

            [ReadOnly]
            public NativeArray<AttackSpawnData> SpawnDataArray;

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

        private struct AttackSpawnData
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

        private NativeList<AttackSpawnData> m_EntitySpawnList;

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
                ComponentType.ReadOnly<AttackInstance>(),
                ComponentType.ReadOnly<Position>(),
                ComponentType.ReadOnly<Rotation>(),
                ComponentType.ReadOnly<Scale>(),
                // ComponentType.ReadOnly<RenderMesh>(),
                ComponentType.ReadOnly<Speed>(),
                ComponentType.ReadOnly<Direction>(),
                ComponentType.ReadOnly<Velocity>(),
                ComponentType.ReadOnly<Damage>(),
                ComponentType.ReadOnly<MaxSqrDistance>(),
                ComponentType.ReadOnly<Prefab>());

            m_AttackedArchetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<Attacked>());

            m_Prefab = EntityManager.CreateEntity(m_Archetype);

            /*
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            var material = sphere.GetComponent<MeshRenderer>().sharedMaterial;
            */

            EntityManager.SetComponentData(m_Prefab, new Scale { Value = new float3(0.25f, 0.25f, 0.25f) });

            /*
            EntityManager.SetSharedComponentData(m_Prefab, new RenderMesh
            {
                mesh = mesh,
                material = material,
                subMesh = 0,
                layer = 0,
                castShadows = UnityEngine.Rendering.ShadowCastingMode.On,
                receiveShadows = true
            });
            */

            // Object.Destroy(sphere);

            m_EntitySpawnList = new NativeList<AttackSpawnData>(Allocator.Persistent);

            m_Random = new MRandom((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            var barrier = World.GetExistingManager<EndFrameBarrier>();

            m_EntitySpawnList.Clear();

            JobHandle inputDeps = default;

            inputDeps = new ConsolidateJob
            {
                Time = Time.time,
                ChunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob),
                EntityType = GetArchetypeChunkEntityType(),
                TargetType = GetArchetypeChunkComponentType<Target>(true),
                PositionType = GetArchetypeChunkComponentType<Position>(true),
                AttackType = GetArchetypeChunkComponentType<Attack>(true),
                AttackDistanceType = GetArchetypeChunkComponentType<AttackDistance>(true),
                AttackDurationType = GetArchetypeChunkComponentType<AttackDuration>(true),
                AttackSpeedType = GetArchetypeChunkComponentType<AttackSpeed>(true),
                EntitySpawnList = m_EntitySpawnList,
                PositionFromEntity = GetComponentDataFromEntity<Position>(true),
                AttackedArchetype = m_AttackedArchetype,
                EntityCommandBuffer = barrier.CreateCommandBuffer()
            }.Schedule(inputDeps);

            inputDeps.Complete();

            var entitySpawnArray = new NativeArray<Entity>(m_EntitySpawnList.Length, Allocator.TempJob);

            EntityManager.Instantiate(m_Prefab, entitySpawnArray);

            inputDeps = new ApplyJob
            {
                AttackInstanceFromEntity = GetComponentDataFromEntity<AttackInstance>(),
                PositionFromEntity = GetComponentDataFromEntity<Position>(),
                RotationFromEntity = GetComponentDataFromEntity<Rotation>(),
                DirectionFromEntity = GetComponentDataFromEntity<Direction>(),
                MaxSqrDistanceFromEntity = GetComponentDataFromEntity<MaxSqrDistance>(),
                SpeedFromEntity = GetComponentDataFromEntity<Speed>(),
                DamageFromEntity = GetComponentDataFromEntity<Damage>(),
                VelocityFromEntity = GetComponentDataFromEntity<Velocity>(),
                EntityArray = entitySpawnArray,
                SpawnDataArray = m_EntitySpawnList.AsDeferredJobArray()
            }.Schedule(entitySpawnArray.Length, 64, inputDeps);

            inputDeps.Complete();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_EntitySpawnList.IsCreated)
            {
                m_EntitySpawnList.Dispose();
            }
        }
    }
}