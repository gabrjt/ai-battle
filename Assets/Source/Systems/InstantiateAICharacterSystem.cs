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
                ComponentType.ReadWrite<MaxHealth>(),
                ComponentType.ReadWrite<Health>(),
                ComponentType.ReadWrite<HealthRegeneration>(),
                ComponentType.ReadWrite<ViewInfo>(),
                ComponentType.ReadWrite<ViewVisible>(),
                ComponentType.ReadWrite<MaxSqrViewDistanceFromCamera>()); // TODO: ViewVisibleSystem

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
                var movementSpeed = new MovementSpeed();
                var engageSqrRadius = new EngageSqrRadius();
                var attackAnimationDuration = new AttackAnimationDuration();
                var attackDamage = new AttackDamage();
                var attackSpeed = new AttackSpeed();
                var maxHealth = new MaxHealth();
                var health = new Health();
                var healthRegeneration = new HealthRegeneration();
                var viewInfo = new ViewInfo();
                var maxSqrViewDistanceFromCamera = new MaxSqrViewDistanceFromCamera { Value = 10000 };

                PostUpdateCommands.SetComponent(entity, new Translation { Value = terrain.GetRandomPosition() });

                switch (type)
                {
                    case ViewType.Knight:
                        PostUpdateCommands.AddComponent(entity, new Knight());
                        movementSpeed.Value = m_Random.NextFloat(1, 3);
                        engageSqrRadius.Value = m_Random.NextFloat(400, 2500);
                        attackAnimationDuration.Value = 1;
                        attackDamage.Value = m_Random.NextInt(10, 30);
                        attackSpeed.Value = m_Random.NextFloat(1, 3);
                        maxHealth.Value = m_Random.NextInt(100, 200);
                        health.Value = maxHealth.Value;
                        healthRegeneration.Value = m_Random.NextFloat(1, 3);
                        viewInfo.Type = ViewType.Knight;
                        EntityManager.SetName(entity, $"{viewInfo.Type} {entity}");
                        break;

                    case ViewType.OrcWolfRider:
                        PostUpdateCommands.AddComponent(entity, new OrcWolfRider());
                        movementSpeed.Value = m_Random.NextFloat(1, 3);
                        engageSqrRadius.Value = m_Random.NextFloat(400, 2500);
                        attackAnimationDuration.Value = 1;
                        attackDamage.Value = m_Random.NextInt(10, 30);
                        attackSpeed.Value = m_Random.NextFloat(1, 3);
                        maxHealth.Value = m_Random.NextInt(100, 200);
                        health.Value = maxHealth.Value;
                        healthRegeneration.Value = m_Random.NextFloat(1, 3);
                        viewInfo.Type = ViewType.OrcWolfRider;
                        EntityManager.SetName(entity, $"{viewInfo.Type} {entity}");
                        break;

                    case ViewType.Skeleton:
                        PostUpdateCommands.AddComponent(entity, new Skeleton());
                        movementSpeed.Value = m_Random.NextFloat(1, 3);
                        engageSqrRadius.Value = m_Random.NextFloat(400, 2500);
                        attackAnimationDuration.Value = 1;
                        attackDamage.Value = m_Random.NextInt(10, 30);
                        attackSpeed.Value = m_Random.NextFloat(1, 3);
                        maxHealth.Value = m_Random.NextInt(100, 200);
                        health.Value = maxHealth.Value;
                        healthRegeneration.Value = m_Random.NextFloat(1, 3);
                        viewInfo.Type = ViewType.Skeleton;
                        EntityManager.SetName(entity, $"{viewInfo.Type} {entity}");
                        break;
                }

                PostUpdateCommands.SetComponent(entity, movementSpeed);
                PostUpdateCommands.SetComponent(entity, engageSqrRadius);
                PostUpdateCommands.SetComponent(entity, attackAnimationDuration);
                PostUpdateCommands.SetComponent(entity, attackDamage);
                PostUpdateCommands.SetComponent(entity, attackSpeed);
                PostUpdateCommands.SetComponent(entity, maxHealth);
                PostUpdateCommands.SetComponent(entity, health);
                PostUpdateCommands.SetComponent(entity, healthRegeneration);
                PostUpdateCommands.SetComponent(entity, viewInfo);
                PostUpdateCommands.SetSharedComponent(entity, maxSqrViewDistanceFromCamera);
                EntityManager.SetName(entity, $"{viewInfo.Type} AI {entity}");
            }

            entityArray.Dispose();
        }
    }
}