﻿using Game.Components;
using Game.Enums;
using Game.Extensions;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [AlwaysUpdateSystem]
    [UpdateAfter(typeof(SetCharacterCountInputFieldSingletonSystem))]
    [UpdateAfter(typeof(DestroyBarrier))]
    public class SpawnAICharacterSystem : ComponentSystem
    {
        [BurstCompile]
        private struct SetDataJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<HomePosition> HomePositionFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Position> PositionFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Rotation> RotationFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<MaxHealth> MaxHealthFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Health> HealthFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Attack> AttackFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<AttackSpeed> AttackSpeedFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<HealthRegeneration> HealthRegenerationFromEntity;

            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<AttackDuration> AttackDurationFromEntity;

            [ReadOnly]
            public NativeArray<Entity> EntityArray;

            [ReadOnly]
            public NativeArray<SetData> SetDataArray;

            public void Execute(int index)
            {
                var entity = EntityArray[index];
                var setData = SetDataArray[index];

                HomePositionFromEntity[entity] = setData.HomePosition;
                PositionFromEntity[entity] = setData.Position;
                RotationFromEntity[entity] = setData.Rotation;
                MaxHealthFromEntity[entity] = setData.MaxHealth;
                HealthFromEntity[entity] = setData.Health;
                AttackFromEntity[entity] = setData.Attack;
                AttackSpeedFromEntity[entity] = setData.AttackSpeed;
                HealthRegenerationFromEntity[entity] = setData.HealthRegeneration;
                AttackDurationFromEntity[entity] = setData.AttackDuration;
            }
        }

        private struct AddGroupJob : IJob
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> EntityArray;

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<SetData> SetDataArray;

            [ReadOnly]
            public EntityCommandBuffer CommandBuffer;

            public void Execute()
            {
                for (var index = 0; index < EntityArray.Length; index++)
                {
                    var entity = EntityArray[index];
                    var viewType = SetDataArray[index].ViewType;

                    switch (viewType)
                    {
                        case ViewType.Knight:
                            CommandBuffer.AddComponent(entity, new Knight());
                            CommandBuffer.AddComponent(entity, new Group { Value = (int)viewType });
                            break;

                        case ViewType.OrcWolfRider:
                            CommandBuffer.AddComponent(entity, new OrcWolfRider());
                            CommandBuffer.AddComponent(entity, new Group { Value = (int)viewType });
                            break;

                        case ViewType.Skeleton:
                            CommandBuffer.AddComponent(entity, new Skeleton());
                            CommandBuffer.AddComponent(entity, new Group { Value = (int)viewType });
                            break;
                    }
                }
            }
        }

        private struct SetData
        {
            public HomePosition HomePosition;

            public Position Position;

            public Rotation Rotation;

            public MaxHealth MaxHealth;

            public Health Health;

            public Attack Attack;

            public AttackSpeed AttackSpeed;

            public HealthRegeneration HealthRegeneration;

            public ViewType ViewType;

            public AttackDuration AttackDuration;
        }

        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private GameObject m_Prefab;

        private GameObject m_ViewPrefab;

        internal int m_TotalCount = 50;

        internal int m_LastTotalCount;

        private Random m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Attach>());

            m_LastTotalCount = m_TotalCount;

            m_Random = new Random((uint)System.Environment.TickCount);

            Debug.Assert(m_Prefab = Resources.Load<GameObject>("AI Character"));
        }

        protected override void OnUpdate()
        {
            var terrain = Terrain.activeTerrain;
            var count = m_Group.CalculateLength();

            SpawnAICharacters(terrain, count);
        }

        private void SpawnAICharacters(Terrain terrain, int count)
        {
            var entityCount = m_TotalCount - count;

            if (entityCount <= 0) return;

            var characterPool = World.GetExistingManager<DestroyBarrier>().m_CharacterPool;

            var entityArray = new NativeArray<Entity>(entityCount, Allocator.TempJob);
            var setDataArray = new NativeArray<SetData>(entityCount, Allocator.TempJob);

            for (var entityIndex = 0; entityIndex < entityCount; entityIndex++)
            {
                GameObject gameObject = null;

                if (characterPool.Count > 0)
                {
                    gameObject = characterPool.Dequeue();
                }
                else
                {
                    gameObject = Object.Instantiate(m_Prefab);
                }

                gameObject.SetActive(true);

                gameObject.GetComponent<CapsuleCollider>().enabled = true;

                var navMeshAgent = gameObject.GetComponent<NavMeshAgent>();
                navMeshAgent.enabled = true;
                navMeshAgent.Warp(terrain.GetRandomPosition());
                navMeshAgent.transform.rotation = m_Random.NextQuaternionRotation();

                var entity = gameObject.GetComponent<GameObjectEntity>().Entity;

                navMeshAgent.name = $"Character AI {entity.Index}";

                var maxHealth = m_Random.NextInt(100, 301);
                var attack = m_Random.NextInt(10, 31);
                var attackSpeed = m_Random.NextFloat(1, 3);
                var healthRegeneration = m_Random.NextFloat(1, 6);

                var viewTypeIndex = m_Random.NextInt(0, 3);
                var viewType = default(ViewType);
                var attackDuration = 0f;

                switch (viewTypeIndex)
                {
                    case 0:
                        viewType = ViewType.Knight;
                        attackDuration = 1;
                        break;

                    case 1:
                        viewType = ViewType.OrcWolfRider;
                        attackDuration = 1.333f;
                        break;

                    case 2:
                        viewType = ViewType.Skeleton;
                        attackDuration = 2f;
                        break;

                    default:
                        break;
                }

                entityArray[entityIndex] = entity;

                var setData = new SetData
                {
                    HomePosition = new HomePosition { Value = navMeshAgent.transform.position },

                    Position = new Position { Value = navMeshAgent.transform.position },
                    Rotation = new Rotation { Value = navMeshAgent.transform.rotation },

                    MaxHealth = new MaxHealth { Value = maxHealth },
                    Health = new Health { Value = maxHealth },

                    Attack = new Attack { Value = attack },
                    AttackSpeed = new AttackSpeed { Value = attackSpeed },
                    AttackDuration = new AttackDuration { Value = attackDuration },

                    HealthRegeneration = new HealthRegeneration { Value = healthRegeneration },

                    ViewType = viewType
                };

                setDataArray[entityIndex] = setData;
            }

            var setBarrier = World.GetExistingManager<SetBarrier>();

            var inputDeps = default(JobHandle);

            inputDeps = new SetDataJob
            {
                EntityArray = entityArray,
                SetDataArray = setDataArray,
                HomePositionFromEntity = GetComponentDataFromEntity<HomePosition>(),
                PositionFromEntity = GetComponentDataFromEntity<Position>(),
                RotationFromEntity = GetComponentDataFromEntity<Rotation>(),
                MaxHealthFromEntity = GetComponentDataFromEntity<MaxHealth>(),
                HealthFromEntity = GetComponentDataFromEntity<Health>(),
                AttackFromEntity = GetComponentDataFromEntity<Attack>(),
                AttackSpeedFromEntity = GetComponentDataFromEntity<AttackSpeed>(),
                HealthRegenerationFromEntity = GetComponentDataFromEntity<HealthRegeneration>(),
                AttackDurationFromEntity = GetComponentDataFromEntity<AttackDuration>()
            }.Schedule(setDataArray.Length, 64, inputDeps);

            inputDeps = new AddGroupJob
            {
                EntityArray = entityArray,
                SetDataArray = setDataArray,
                CommandBuffer = setBarrier.CreateCommandBuffer(),
            }.Schedule(inputDeps);

            inputDeps.Complete();
        }
    }
}