using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class SpawnProjectileSystem : ComponentSystem
    {
        private struct ProjectileSpawnData
        {
            public Position Position;

            public Rotation Rotation;

            public Direction Direction;
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private Entity m_Prefab;

        private NativeList<ProjectileSpawnData> m_EntitySpawnList;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Position>() },
                None = new[] { ComponentType.ReadOnly<Cooldown>() }
            });

            m_Archetype = EntityManager.CreateArchetype(
                ComponentType.ReadOnly<Projectile>(),
                ComponentType.ReadOnly<Position>(),
                ComponentType.ReadOnly<Rotation>(),
                ComponentType.ReadOnly<Scale>(),
                ComponentType.ReadOnly<RenderMesh>(),
                ComponentType.ReadOnly<Speed>(),
                ComponentType.ReadOnly<Direction>(),
                ComponentType.ReadOnly<Velocity>(),
                ComponentType.ReadOnly<Damage>(),
                ComponentType.ReadOnly<Prefab>());

            m_Prefab = EntityManager.CreateEntity(m_Archetype);

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
            var material = sphere.GetComponent<MeshRenderer>().sharedMaterial;

            EntityManager.SetComponentData(m_Prefab, new Scale { Value = new float3(0.25f, 0.25f, 0.25f) });
            EntityManager.SetSharedComponentData(m_Prefab, new RenderMesh
            {
                mesh = mesh,
                material = material,
                subMesh = 0,
                layer = 0,
                castShadows = UnityEngine.Rendering.ShadowCastingMode.On,
                receiveShadows = true
            });
            EntityManager.SetComponentData(m_Prefab, new Speed { Value = 5 });
            EntityManager.SetComponentData(m_Prefab, new Damage { Value = 10 });

            Object.Destroy(sphere);

            m_EntitySpawnList = new NativeList<ProjectileSpawnData>(Allocator.Persistent);
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var entityType = GetArchetypeChunkEntityType();
            var targetType = GetArchetypeChunkComponentType<Target>(true);
            var positionType = GetArchetypeChunkComponentType<Position>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var entityArray = chunk.GetNativeArray(entityType);
                var targetArray = chunk.GetNativeArray(targetType);
                var positionArray = chunk.GetNativeArray(positionType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var target = targetArray[entityIndex];
                    var position = positionArray[entityIndex];
                    var direction = math.normalizesafe(EntityManager.GetComponentData<Position>(target.Value).Value - position.Value);

                    m_EntitySpawnList.Add(new ProjectileSpawnData
                    {
                        Position = new Position { Value = position.Value + new float3(0, 0.35f, 0) },
                        Rotation = new Rotation { Value = quaternion.LookRotation(direction, Vector3.up) },
                        Direction = new Direction { Value = direction }
                    });

                    PostUpdateCommands.AddComponent(entityArray[entityIndex], new Cooldown
                    {
                        Value = 1,
                        StartTime = Time.time
                    });
                }
            }

            chunkArray.Dispose();

            var entitySpawnArray = new NativeArray<Entity>(m_EntitySpawnList.Length, Allocator.TempJob);

            EntityManager.Instantiate(m_Prefab, entitySpawnArray);

            for (var i = 0; i < entitySpawnArray.Length; i++)
            {
                var spawnData = m_EntitySpawnList[i];
                var entity = entitySpawnArray[i];
                EntityManager.SetComponentData(entity, spawnData.Position);
                EntityManager.SetComponentData(entity, spawnData.Rotation);
                EntityManager.SetComponentData(entity, spawnData.Direction);
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