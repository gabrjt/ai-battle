using Game.Components;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Game.Systems
{
    [UpdateBefore(typeof(DamageSystem))]
    public class AttackCollisionSystem : ComponentSystem
    {
        private struct ConsolidateJob : IJobParallelFor
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;

            public NativeArray<SpherecastCommandData> SphereCastCommandDataArray;

            [ReadOnly]
            public ComponentDataFromEntity<AttackInstance> AttackInstanceFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Direction> DirectionFromEntity;

            [ReadOnly]
            public ComponentDataFromEntity<Speed> SpeedFromEntity;

            [ReadOnly]
            public float DeltaTime;

            public void Execute(int index)
            {
                var entity = EntityArray[index];

                var attackInstanceFromEntity = AttackInstanceFromEntity[entity];

                SphereCastCommandDataArray[index] = new SpherecastCommandData
                {
                    Entity = entity,
                    Owner = attackInstanceFromEntity.Owner,
                    Origin = PositionFromEntity[entity].Value,
                    Direction = DirectionFromEntity[entity].Value,
                    Distance = SpeedFromEntity[entity].Value * DeltaTime,
                    Radius = attackInstanceFromEntity.Radius
                };
            }
        }

        private struct SpherecastCommandData
        {
            public Entity Entity;

            public Entity Owner;

            public float3 Origin;

            public float3 Direction;

            public float Distance;

            public float Radius;
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_DamagedArchetype;

        private EntityArchetype m_CollidedArchetype;

        private NativeArray<SpherecastCommandData> m_SphereCastCommandDataArray;

        private NativeArray<SpherecastCommand> m_CommandArray;

        private NativeArray<RaycastHit> m_ResultArray;

        private LayerMask m_LayerMask;

        private int m_Layer;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<AttackInstance>(), ComponentType.ReadOnly<Direction>(), ComponentType.ReadOnly<Position>(), ComponentType.ReadOnly<Speed>() }
            });

            m_DamagedArchetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<Damaged>());
            m_CollidedArchetype = EntityManager.CreateArchetype(ComponentType.Create<Components.Event>(), ComponentType.Create<Collided>());

            m_LayerMask = LayerMask.NameToLayer("Entity");
            m_Layer = 1 << m_LayerMask;
        }

        protected override void OnUpdate()
        {
            var entityCommandBuffer = World.GetExistingManager<EndFrameBarrier>().CreateCommandBuffer();

            var entityArray = m_Group.ToEntityArray(Allocator.TempJob);

            m_SphereCastCommandDataArray = new NativeArray<SpherecastCommandData>(entityArray.Length, Allocator.TempJob);

            var inputDeps = default(JobHandle);

            inputDeps = new ConsolidateJob
            {
                EntityArray = entityArray,
                SphereCastCommandDataArray = m_SphereCastCommandDataArray,
                AttackInstanceFromEntity = GetComponentDataFromEntity<AttackInstance>(true),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true),
                DirectionFromEntity = GetComponentDataFromEntity<Direction>(true),
                SpeedFromEntity = GetComponentDataFromEntity<Speed>(true),
                DeltaTime = Time.deltaTime
            }.Schedule(entityArray.Length, 64, inputDeps);

            inputDeps.Complete();

            m_CommandArray = new NativeArray<SpherecastCommand>(m_SphereCastCommandDataArray.Length, Allocator.TempJob);
            m_ResultArray = new NativeArray<RaycastHit>(m_SphereCastCommandDataArray.Length, Allocator.TempJob);

            for (var i = 0; i < m_SphereCastCommandDataArray.Length; i++)
            {
                var sphereCastCommand = m_SphereCastCommandDataArray[i];
                m_CommandArray[i] = new SpherecastCommand(sphereCastCommand.Origin, sphereCastCommand.Radius, sphereCastCommand.Direction, sphereCastCommand.Distance, m_Layer);
            }

            SpherecastCommand.ScheduleBatch(m_CommandArray, m_ResultArray, 1).Complete();

            for (int i = 0; i < m_ResultArray.Length; i++)
            {
                var result = m_ResultArray[i];

                if (!result.collider) continue;

                var entity = m_SphereCastCommandDataArray[i].Entity;
                var owner = m_SphereCastCommandDataArray[i].Owner;

                EntityManager.AddComponentData(entity, new Disabled());

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

            m_SphereCastCommandDataArray.Dispose();
            m_CommandArray.Dispose();
            m_ResultArray.Dispose();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SphereCastCommandDataArray.IsCreated)
            {
                m_SphereCastCommandDataArray.Dispose();
            }

            if (m_CommandArray.IsCreated)
            {
                m_CommandArray.Dispose();
            }

            if (m_ResultArray.IsCreated)
            {
                m_ResultArray.Dispose();
            }
        }
    }
}