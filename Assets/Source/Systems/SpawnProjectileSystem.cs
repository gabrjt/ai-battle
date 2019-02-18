using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

        private NativeList<ProjectileSpawnData> m_EntitySpawnList;

        private GameObject m_Prefab;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Target>(), ComponentType.ReadOnly<Position>() }
            });

            m_EntitySpawnList = new NativeList<ProjectileSpawnData>(Allocator.Persistent);

            Debug.Assert(m_Prefab = Resources.Load<GameObject>("Projectile"));
        }

        protected override void OnUpdate()
        {
            var chunkArray = m_Group.CreateArchetypeChunkArray(Allocator.TempJob);
            var targetType = GetArchetypeChunkComponentType<Target>(true);
            var positionType = GetArchetypeChunkComponentType<Position>(true);

            for (var chunkIndex = 0; chunkIndex < chunkArray.Length; chunkIndex++)
            {
                var chunk = chunkArray[chunkIndex];
                var targetArray = chunk.GetNativeArray(targetType);
                var positionArray = chunk.GetNativeArray(positionType);

                for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var target = targetArray[entityIndex];
                    var position = positionArray[entityIndex];
                    var direction = math.normalizesafe(EntityManager.GetComponentData<Position>(target.Value).Value - position.Value);

                    m_EntitySpawnList.Add(new ProjectileSpawnData
                    {
                        Position = new Position { Value = position.Value + new float3(0, 0.5f, 0.5f) },
                        Rotation = new Rotation { Value = quaternion.LookRotation(direction, Vector3.up) },
                        Direction = new Direction { Value = direction }
                    });
                }
            }

            chunkArray.Dispose();

            for (var i = 0; i < m_EntitySpawnList.Length; i++)
            {
                var spawnData = m_EntitySpawnList[i];
                var projectileEntity = Object.Instantiate(m_Prefab).GetComponent<GameObjectEntity>().Entity;
                PostUpdateCommands.SetComponent(projectileEntity, spawnData.Position);
                PostUpdateCommands.SetComponent(projectileEntity, spawnData.Rotation);
                PostUpdateCommands.SetComponent(projectileEntity, spawnData.Direction);
                PostUpdateCommands.SetComponent(projectileEntity, new Velocity { Value = spawnData.Direction.Value * EntityManager.GetComponentData<Speed>(projectileEntity).Value });
            }

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