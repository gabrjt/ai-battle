using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    public class SpawnAttackSystem : ComponentSystem
    {
        private struct AttackInstanceSpawnData
        {
            public Entity Owner;

            public Position Position;

            public Rotation Rotation;

            public Direction Direction;

            public MaximumDistance MaximumDistance;

            public Speed Speed;
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private EntityArchetype m_AttackedArchetype;

        private Entity m_Prefab;

        private NativeList<AttackInstanceSpawnData> m_EntitySpawnList;

        private MRandom m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<AttackDistance>(), ComponentType.ReadOnly<AttackSpeed>() },
                None = new[] { ComponentType.ReadOnly<Cooldown>(), ComponentType.ReadOnly<Attack>() }
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
                ComponentType.ReadOnly<MaximumDistance>(),
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

            m_EntitySpawnList = new NativeList<AttackInstanceSpawnData>(Allocator.Persistent);

            m_Random = new MRandom((uint)System.Environment.TickCount);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var targetType = GetArchetypeChunkComponentType<Target>(true);
            var positionType = GetArchetypeChunkComponentType<Position>(true);
            var attackDistanceType = GetArchetypeChunkComponentType<AttackDistance>(true);
            var attackSpeedType = GetArchetypeChunkComponentType<AttackSpeed>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);
                var targetArray = chunk.GetNativeArray(targetType);
                var positionArray = chunk.GetNativeArray(positionType);
                var attackDistanceArray = chunk.GetNativeArray(attackDistanceType);
                var attackSpeedArray = chunk.GetNativeArray(attackSpeedType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var target = targetArray[entityIndex];

                    if (!EntityManager.HasComponent<Position>(target.Value) || !EntityManager.Exists(target.Value)) continue;

                    var position = positionArray[entityIndex].Value;
                    var targetPosition = EntityManager.GetComponentData<Position>(target.Value).Value;
                    var attackDistance = attackDistanceArray[entityIndex];
                    var attackSpeed = attackSpeedArray[entityIndex].Value;

                    if (math.distance(position, targetPosition) > attackDistance.Maximum) continue;

                    var direction = math.normalizesafe(targetPosition - position);

                    var origin = position + new float3(0, 0.35f, 0);

                    m_EntitySpawnList.Add(new AttackInstanceSpawnData
                    {
                        Owner = entity,
                        Position = new Position { Value = origin },
                        Rotation = new Rotation { Value = quaternion.LookRotation(direction, math.up()) },
                        Direction = new Direction { Value = direction },
                        MaximumDistance = new MaximumDistance
                        {
                            Origin = position,
                            Value = attackDistance.Maximum
                        },
                        Speed = new Speed { Value = attackSpeed * 5 }
                    });

                    var duration = 1.333f / attackSpeed;

                    PostUpdateCommands.AddComponent(entity, new Attack
                    {
                        Value = duration,
                        StartTime = Time.time
                    });

                    PostUpdateCommands.AddComponent(entity, new Cooldown
                    {
                        Value = duration,
                        StartTime = Time.time
                    });

                    var attacked = PostUpdateCommands.CreateEntity(m_AttackedArchetype);
                    PostUpdateCommands.SetComponent(attacked, new Attacked { This = entity });
                }
            }

            chunkArray.Dispose();

            var entitySpawnArray = new NativeArray<Entity>(m_EntitySpawnList.Length, Allocator.TempJob);

            EntityManager.Instantiate(m_Prefab, entitySpawnArray);

            for (var i = 0; i < entitySpawnArray.Length; i++)
            {
                var spawnData = m_EntitySpawnList[i];
                var entity = entitySpawnArray[i];

                EntityManager.SetComponentData(entity, new AttackInstance
                {
                    Owner = spawnData.Owner,
                    Radius = 0.25f
                });
                EntityManager.SetComponentData(entity, spawnData.Position);
                EntityManager.SetComponentData(entity, spawnData.Rotation);
                EntityManager.SetComponentData(entity, spawnData.Direction);
                EntityManager.SetComponentData(entity, spawnData.MaximumDistance);
                EntityManager.SetComponentData(entity, spawnData.Speed);
                EntityManager.SetComponentData(entity, new Damage { Value = m_Random.NextInt(10, 31) });
                EntityManager.SetComponentData(entity, new Velocity { Value = spawnData.Direction.Value * EntityManager.GetComponentData<Speed>(entity).Value });
            }

            entitySpawnArray.Dispose();

            m_EntitySpawnList.Clear();
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