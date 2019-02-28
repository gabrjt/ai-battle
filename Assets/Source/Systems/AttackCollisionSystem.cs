using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateBefore(typeof(DamageSystem))]
    public class AttackCollisionSystem : ComponentSystem
    {
        private struct SpherecastCommandData
        {
            public Entity Entity;

            public Entity Owner;

            public float3 Origin;

            public float3 Direction;

            public float Distance;

            public float Radius;
        }

        private EntityArchetype m_DamagedArchetype;

        private EntityArchetype m_CollidedArchetype;

        private NativeList<SpherecastCommandData> m_SphereCastCommandDataList;

        private NativeArray<SpherecastCommand> m_CommandArray;

        private NativeArray<RaycastHit> m_ResultArray;

        private LayerMask m_LayerMask;

        private int m_Layer;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_DamagedArchetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<Damaged>());
            m_CollidedArchetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<Collided>());

            m_SphereCastCommandDataList = new NativeList<SpherecastCommandData>(Allocator.Persistent);

            m_LayerMask = LayerMask.NameToLayer("Entity");
            m_Layer = 1 << m_LayerMask;
        }

        protected override void OnUpdate()
        {
            var entityCommandBuffer = World.GetExistingManager<EndFrameBarrier>().CreateCommandBuffer();

            ForEach((Entity entity, ref AttackInstance AttackInstance, ref Direction direction, ref Position position, ref Speed speed) =>
            {
                m_SphereCastCommandDataList.Add(new SpherecastCommandData
                {
                    Entity = entity,
                    Owner = AttackInstance.Owner,
                    Origin = position.Value,
                    Direction = direction.Value,
                    Distance = speed.Value * Time.deltaTime,
                    Radius = AttackInstance.Radius
                });
            });

            m_CommandArray = new NativeArray<SpherecastCommand>(m_SphereCastCommandDataList.Length, Allocator.TempJob);
            m_ResultArray = new NativeArray<RaycastHit>(m_SphereCastCommandDataList.Length, Allocator.TempJob);

            for (var i = 0; i < m_SphereCastCommandDataList.Length; i++)
            {
                var sphereCastCommand = m_SphereCastCommandDataList[i];
                m_CommandArray[i] = new SpherecastCommand(sphereCastCommand.Origin, sphereCastCommand.Radius, sphereCastCommand.Direction, sphereCastCommand.Distance, m_Layer);
            }

            SpherecastCommand.ScheduleBatch(m_CommandArray, m_ResultArray, 1).Complete();

            for (int i = 0; i < m_ResultArray.Length; i++)
            {
                var result = m_ResultArray[i];

                if (!result.collider) continue;

                var entity = m_SphereCastCommandDataList[i].Entity;
                var owner = m_SphereCastCommandDataList[i].Owner;

                var damage = EntityManager.GetComponentData<Damage>(entity).Value;
                var target = result.collider.GetComponent<GameObjectEntity>().Entity;

                var damaged = entityCommandBuffer.CreateEntity(m_DamagedArchetype);
                entityCommandBuffer.SetComponent(damaged, new Damaged
                {
                    Value = damage,
                    This = owner,
                    Other = target
                });

                var collided = entityCommandBuffer.CreateEntity(m_CollidedArchetype);
                entityCommandBuffer.SetComponent(collided, new Collided
                {
                    This = entity,
                    Other = target
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