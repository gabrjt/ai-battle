using Game.Comparers;
using Game.Components;
using Game.Enums;
using Game.Extensions;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(InstantiateGroup))]
    public class InstantiateAICharacterSystem : ComponentSystem
    {
        private struct SetData
        {
            public HomePosition HomePosition;
            public Translation Translation;
            public Rotation Rotation;
            public MaxHealth MaxHealth;
            public Health Health;
            public Attack Attack;
            public AttackSpeed AttackSpeed;
            public HealthRegeneration HealthRegeneration;
            public ViewType ViewType;
            public AttackDuration AttackDuration;
        }

        [BurstCompile]
        private struct SetDataJob : IJobParallelFor
        {
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<HomePosition> HomePositionFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Translation> PositionFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Rotation> RotationFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<MaxHealth> MaxHealthFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Health> HealthFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<Attack> AttackFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AttackSpeed> AttackSpeedFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<HealthRegeneration> HealthRegenerationFromEntity;
            [NativeDisableParallelForRestriction] public ComponentDataFromEntity<AttackDuration> AttackDurationFromEntity;
            [ReadOnly] public NativeArray<Entity> EntityArray;
            [ReadOnly] public NativeArray<SetData> SetDataArray;

            public void Execute(int index)
            {
                var entity = EntityArray[index];
                var setData = SetDataArray[index];

                HomePositionFromEntity[entity] = setData.HomePosition;
                PositionFromEntity[entity] = setData.Translation;
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
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<Entity> EntityArray;
            [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<SetData> SetDataArray;
            [ReadOnly] public EntityCommandBuffer CommandBuffer;

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

        private ComponentGroup m_Group;
        private ComponentGroup m_KnightGroup;
        private ComponentGroup m_OrcWolfRiderGroup;
        private ComponentGroup m_SkeletonGroup;
        private EntityArchetype m_Archetype;
        private GameObject m_Prefab;
        private GameObject m_ViewPrefab;
        internal int m_TotalCount = 0xFFF;
        internal int m_LastTotalCount;
        private Random m_Random;
        private readonly CharacterCountComparer m_CharacterCountComparer = new CharacterCountComparer();

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() }
            });

            m_KnightGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Knight>() }
            });

            m_OrcWolfRiderGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<OrcWolfRider>() }
            });

            m_SkeletonGroup = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>(), ComponentType.ReadOnly<Skeleton>() }
            });

            m_Archetype = EntityManager.CreateArchetype(

                ComponentType.ReadWrite<Character>(),
                ComponentType.ReadWrite<HomePosition>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<MaxHealth>(),
                ComponentType.ReadWrite<Health>(),
                ComponentType.ReadWrite<Attack>(),
                ComponentType.ReadWrite<AttackSpeed>(),
                ComponentType.ReadWrite<AttackDuration>(),
                ComponentType.ReadWrite<HealthRegeneration>()
            );

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

            var entityArray = new NativeArray<Entity>(entityCount, Allocator.TempJob);
            var setDataArray = new NativeArray<SetData>(entityCount, Allocator.TempJob);
            var knightCount = m_KnightGroup.CalculateLength();
            var orcWolfRiderCount = m_OrcWolfRiderGroup.CalculateLength();
            var skeletonCount = m_SkeletonGroup.CalculateLength();

            for (var entityIndex = 0; entityIndex < entityCount; entityIndex++)
            {
                var sortArray = new CharacterCountSortData[]
                {
                     new CharacterCountSortData
                     {
                         ViewType = ViewType.Knight,
                         Count = knightCount
                     }, new CharacterCountSortData
                     {
                         ViewType = ViewType.OrcWolfRider,
                         Count = orcWolfRiderCount
                     }, new CharacterCountSortData
                     {
                         ViewType = ViewType.Skeleton,
                         Count = skeletonCount
                     }
                };

                Array.Sort(sortArray, 0, sortArray.Length, m_CharacterCountComparer);
                var viewType = sortArray[0].ViewType;
                Array.Clear(sortArray, 0, sortArray.Length);

                var homePosition = terrain.GetRandomPosition();
                var maxHealth = 0;
                var attack = 0;
                var attackSpeed = 0f;
                var healthRegeneration = 0f;
                var attackDuration = 0f;

                switch (viewType)
                {
                    case ViewType.Knight:
                        maxHealth = m_Random.NextInt(300, 751);
                        attack = m_Random.NextInt(25, 31);
                        attackSpeed = m_Random.NextFloat(1, 3);
                        healthRegeneration = m_Random.NextFloat(2, 4);
                        attackDuration = 1;
                        ++knightCount;
                        break;

                    case ViewType.OrcWolfRider:
                        maxHealth = m_Random.NextInt(400, 1001);
                        attack = m_Random.NextInt(30, 51);
                        attackSpeed = m_Random.NextFloat(1, 2);
                        healthRegeneration = m_Random.NextFloat(4, 6);
                        attackDuration = 1.333f;
                        ++orcWolfRiderCount;
                        break;

                    case ViewType.Skeleton:
                        maxHealth = m_Random.NextInt(150, 251);
                        attack = m_Random.NextInt(40, 71);
                        attackSpeed = m_Random.NextFloat(1, 4);
                        healthRegeneration = m_Random.NextFloat(0.5f);
                        attackDuration = 2f;
                        if (m_Random.NextFloat(1) >= 0.9f) ++skeletonCount;
                        break;
                }

                var setData = new SetData
                {
                    HomePosition = new HomePosition { Value = homePosition },
                    Translation = new Translation { Value = homePosition },
                    Rotation = new Rotation { Value = quaternion.identity },
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

            EntityManager.CreateEntity(m_Archetype, entityArray);

            var setCommandBufferSystem = World.GetExistingManager<SetCommandBufferSystem>();
            var inputDeps = default(JobHandle);

            inputDeps = new SetDataJob
            {
                EntityArray = entityArray,
                SetDataArray = setDataArray,
                HomePositionFromEntity = GetComponentDataFromEntity<HomePosition>(),
                PositionFromEntity = GetComponentDataFromEntity<Translation>(),
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
                CommandBuffer = setCommandBufferSystem.CreateCommandBuffer(),
            }.Schedule(inputDeps);

            inputDeps.Complete();
        }
    }
}