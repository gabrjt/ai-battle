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

            public NativeArray<SpherecastCommandData> SpherecastCommandDataArray;
            public NativeArray<SpherecastCommand> CommandArray;

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

            [ReadOnly]
            public int Layer;

            public void Execute(int index)
            {
                var entity = EntityArray[index];

                var attackInstanceFromEntity = AttackInstanceFromEntity[entity];

                var spherecastCommand = new SpherecastCommandData
                {
                    Entity = entity,
                    Owner = attackInstanceFromEntity.Owner,
                    Origin = PositionFromEntity[entity].Value,
                    Direction = DirectionFromEntity[entity].Value,
                    Distance = SpeedFromEntity[entity].Value * DeltaTime,
                    Radius = attackInstanceFromEntity.Radius
                };

                SpherecastCommandDataArray[index] = spherecastCommand;
                CommandArray[index] = new SpherecastCommand(spherecastCommand.Origin, spherecastCommand.Radius, spherecastCommand.Direction, spherecastCommand.Distance, Layer);
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

        private NativeArray<SpherecastCommandData> m_SpherecastCommandDataArray;

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
            var entityArray = m_Group.ToEntityArray(Allocator.TempJob);

            m_SpherecastCommandDataArray = new NativeArray<SpherecastCommandData>(entityArray.Length, Allocator.TempJob);
            m_CommandArray = new NativeArray<SpherecastCommand>(m_SpherecastCommandDataArray.Length, Allocator.TempJob);
            m_ResultArray = new NativeArray<RaycastHit>(m_SpherecastCommandDataArray.Length, Allocator.TempJob);

            var inputDeps = default(JobHandle);

            inputDeps = new ConsolidateJob
            {
                EntityArray = entityArray,
                SpherecastCommandDataArray = m_SpherecastCommandDataArray,
                CommandArray = m_CommandArray,
                AttackInstanceFromEntity = GetComponentDataFromEntity<AttackInstance>(true),
                PositionFromEntity = GetComponentDataFromEntity<Position>(true),
                DirectionFromEntity = GetComponentDataFromEntity<Direction>(true),
                SpeedFromEntity = GetComponentDataFromEntity<Speed>(true),
                DeltaTime = Time.deltaTime,
                Layer = m_Layer
            }.Schedule(entityArray.Length, 64, inputDeps);

            SpherecastCommand.ScheduleBatch(m_CommandArray, m_ResultArray, 1, inputDeps).Complete();

            var entityCommandBuffer = World.GetExistingManager<EndFrameBarrier>().CreateCommandBuffer();

            for (int index = 0; index < m_ResultArray.Length; index++)
            {
                var result = m_ResultArray[index];

                if (!result.collider) continue;

                var entity = m_SpherecastCommandDataArray[index].Entity;
                var owner = m_SpherecastCommandDataArray[index].Owner;

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

            m_SpherecastCommandDataArray.Dispose();
            m_CommandArray.Dispose();
            m_ResultArray.Dispose();
        }

        protected override void OnDestroyManager()
        {
            base.OnDestroyManager();

            if (m_SpherecastCommandDataArray.IsCreated)
            {
                m_SpherecastCommandDataArray.Dispose();
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