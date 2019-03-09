using Game.Components;
using Game.Enums;
using Game.Extensions;
using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Systems
{
    [AlwaysUpdateSystem]
    [UpdateInGroup(typeof(EntityLifecycleGroup))]
    [UpdateAfter(typeof(DestroyEntitySystem))]
    public class InstantiateAICharacterSystem : ComponentSystem
    {
        private ComponentGroup m_Group;
        private EntityArchetype m_Archetype;
        private EntityArchetype m_DestroyAllCharactersArchetype;
        private const int m_MaxDestroyCount = 1024;
        private Random m_Random;
        internal int m_TotalCount = 0xF;
        internal int m_LastTotalCount;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadOnly<Destroy>(), ComponentType.ReadOnly<Disabled>() }
            });

            m_Archetype = EntityManager.CreateArchetype(
                ComponentType.ReadWrite<Character>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<MovementSpeed>(),
                ComponentType.ReadWrite<EngageSqrRadius>(),
                ComponentType.ReadWrite<AttackDistance>(),
                ComponentType.ReadWrite<AttackAnimationDuration>(),
                ComponentType.ReadWrite<AttackDamage>(),
                ComponentType.ReadWrite<AttackSpeed>(),
                ComponentType.ReadWrite<Health>(),
                ComponentType.ReadWrite<MaxHealth>()
            );

            m_DestroyAllCharactersArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<DestroyAllCharacters>());
            m_Random = new Random(0xABCDEF);
            m_LastTotalCount = m_TotalCount;
        }

        protected override void OnUpdate()
        {
            var count = m_Group.CalculateLength();
            var entityCount = m_TotalCount - count;

            DestroyExceedingCharacters(count, entityCount);
            InstantiateCharacters(entityCount);
        }

        private void DestroyExceedingCharacters(int count, int entityCount)
        {
            if (entityCount > 0) return;

            if (m_TotalCount == 0 && count > 0)
            {
                World.GetExistingManager<BeginSimulationEntityCommandBufferSystem>().CreateCommandBuffer().CreateEntity(m_DestroyAllCharactersArchetype);
            }
            else if (entityCount < 0)
            {
                entityCount = math.abs(entityCount);
                var entityArray = m_Group.ToEntityArray(Allocator.TempJob);
                var maxDestroyCount = math.select(entityCount, m_MaxDestroyCount, entityCount > m_MaxDestroyCount);

                for (var entityIndex = 0; entityIndex < maxDestroyCount; entityIndex++)
                {
                    PostUpdateCommands.AddComponent(entityArray[entityIndex], new Destroy());
                    PostUpdateCommands.AddComponent(entityArray[entityIndex], new Disabled());
                }

                entityArray.Dispose();
            }
        }

        private void InstantiateCharacters(int entityCount)
        {
            if (entityCount <= 0) return;

            var entityArray = new NativeArray<Entity>(entityCount, Allocator.TempJob);

            EntityManager.CreateEntity(m_Archetype, entityArray);

            var terrain = Terrain.activeTerrain;

            for (var entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
            {
                var type = (ViewType)m_Random.NextInt(Enum.GetValues(typeof(ViewType)).Length);
                var entity = entityArray[entityIndex];
                var movementSpeed = 0f;
                var engageSqrRadius = 0f;
                var attackAnimationDuration = 0f;
                var attackDamage = 0;
                var attackSpeed = 0f;
                var maxHealth = 0;

                PostUpdateCommands.SetComponent(entity, new Translation { Value = terrain.GetRandomPosition() });

                switch (type)
                {
                    case ViewType.Knight:
                        PostUpdateCommands.AddComponent(entity, new Knight());
                        movementSpeed = m_Random.NextFloat(1, 3);
                        engageSqrRadius = m_Random.NextFloat(400, 2500);
                        attackAnimationDuration = 1;
                        attackDamage = m_Random.NextInt(10, 30);
                        attackSpeed = m_Random.NextFloat(1, 3);
                        maxHealth = m_Random.NextInt(100, 200);
                        break;

                    case ViewType.OrcWolfRider:
                        PostUpdateCommands.AddComponent(entity, new OrcWolfRider());
                        movementSpeed = m_Random.NextFloat(1, 3);
                        engageSqrRadius = m_Random.NextFloat(400, 2500);
                        attackAnimationDuration = 1.333f;
                        attackDamage = m_Random.NextInt(10, 30);
                        attackSpeed = m_Random.NextFloat(1, 3);
                        maxHealth = m_Random.NextInt(100, 200);
                        break;

                    case ViewType.Skeleton:
                        PostUpdateCommands.AddComponent(entity, new Skeleton());
                        movementSpeed = m_Random.NextFloat(1, 3);
                        engageSqrRadius = m_Random.NextFloat(400, 2500);
                        attackAnimationDuration = 2;
                        attackDamage = m_Random.NextInt(10, 30);
                        attackSpeed = m_Random.NextFloat(1, 3);
                        maxHealth = m_Random.NextInt(100, 200);
                        break;
                }

                PostUpdateCommands.SetComponent(entity, new MovementSpeed { Value = movementSpeed });
                PostUpdateCommands.SetComponent(entity, new EngageSqrRadius { Value = engageSqrRadius });
                PostUpdateCommands.SetComponent(entity, new AttackDistance { Min = 1.2f, Max = 1.5f });
                PostUpdateCommands.SetComponent(entity, new AttackAnimationDuration { Value = attackAnimationDuration });
                PostUpdateCommands.SetComponent(entity, new AttackDamage { Value = attackDamage });
                PostUpdateCommands.SetComponent(entity, new AttackSpeed { Value = attackSpeed });
                PostUpdateCommands.SetComponent(entity, new MaxHealth { Value = maxHealth });
                PostUpdateCommands.SetComponent(entity, new Health { Value = maxHealth });
            }

            entityArray.Dispose();
        }
    }
}