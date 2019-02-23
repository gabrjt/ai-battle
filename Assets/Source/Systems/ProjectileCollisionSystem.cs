﻿using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    public class ProjectileCollisionSystem : ComponentSystem
    {
        private struct SpherecastCommandData
        {
            public Entity Entity;

            public float3 Origin;

            public float3 Direction;

            public float Radius;
        }

        private EntityArchetype m_DamagedArchetype;

        private EntityArchetype m_CollidedArchetype;

        private NativeList<SpherecastCommandData> m_SphereCastCommandDataList;

        private NativeArray<SpherecastCommand> m_CommandArray;

        private NativeArray<RaycastHit> m_ResultArray;

        private LayerMask m_LayerMask;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_DamagedArchetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<Damaged>());
            m_CollidedArchetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Components.Event>(), ComponentType.ReadOnly<Collided>());

            m_SphereCastCommandDataList = new NativeList<SpherecastCommandData>(Allocator.Persistent);

            m_LayerMask = LayerMask.NameToLayer("Entity");
        }

        protected override void OnUpdate()
        {
            ForEach((Entity entity, ref Projectile projectile, ref Direction direction, ref Position position) =>
            {
                m_SphereCastCommandDataList.Add(new SpherecastCommandData
                {
                    Entity = entity,
                    Origin = position.Value,
                    Direction = direction.Value,
                    Radius = projectile.Radius
                });
            });

            m_CommandArray = new NativeArray<SpherecastCommand>(m_SphereCastCommandDataList.Length, Allocator.TempJob);
            m_ResultArray = new NativeArray<RaycastHit>(m_SphereCastCommandDataList.Length, Allocator.TempJob);

            for (var i = 0; i < m_SphereCastCommandDataList.Length; i++)
            {
                var sphereCastCommand = m_SphereCastCommandDataList[i];
                m_CommandArray[i] = new SpherecastCommand(sphereCastCommand.Origin, sphereCastCommand.Radius, sphereCastCommand.Direction, sphereCastCommand.Radius * 1.25f, 1 << m_LayerMask);
            }

            SpherecastCommand.ScheduleBatch(m_CommandArray, m_ResultArray, 1).Complete();

            for (int i = 0; i < m_ResultArray.Length; i++)
            {
                var result = m_ResultArray[i];

                if (!result.collider) continue;

                var entity = m_SphereCastCommandDataList[i].Entity;

                var damage = EntityManager.GetComponentData<Damage>(entity).Value;
                var target = result.collider.GetComponent<GameObjectEntity>().Entity;

                var damaged = EntityManager.CreateEntity(m_DamagedArchetype);
                EntityManager.SetComponentData(damaged, new Damaged
                {
                    Value = damage,
                    Source = entity,
                    Target = target
                });

                var collided = EntityManager.CreateEntity(m_CollidedArchetype);
                EntityManager.SetComponentData(collided, new Collided
                {
                    This = entity,
                    Value = target
                });
            }

            m_CommandArray.Dispose();
            m_ResultArray.Dispose();
            m_SphereCastCommandDataList.Clear();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SphereCastCommandDataList.IsCreated)
            {
                m_SphereCastCommandDataList.Dispose();
            }
        }
    }
}