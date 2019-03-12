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
                ComponentType.ReadWrite<LocalToWorld>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<Child>(),
                ComponentType.ReadWrite<MovementSpeed>(),
                ComponentType.ReadWrite<RotationSpeed>(),
                ComponentType.ReadWrite<RotationSpeedModifier>(),
                ComponentType.ReadWrite<WalkSpeedModifier>(),
                ComponentType.ReadWrite<ChargeSpeedModifier>(),
                ComponentType.ReadWrite<EngageSqrRadius>(),
                ComponentType.ReadWrite<AttackDistance>(),
                ComponentType.ReadWrite<AttackAnimationDuration>(),
                ComponentType.ReadWrite<AttackDamage>(),
                ComponentType.ReadWrite<AttackSpeed>(),
                ComponentType.ReadWrite<MaxHealth>(),
                ComponentType.ReadWrite<Health>(),
                ComponentType.ReadWrite<HealthRegeneration>(),
                ComponentType.ReadWrite<ViewInfo>());

            m_DestroyAllCharactersArchetype = EntityManager.CreateArchetype(ComponentType.ReadWrite<Components.Event>(), ComponentType.ReadWrite<DestroyAllCharacters>());
            m_Random = new Random(0xABCDEF);

            RequireSingletonForUpdate<CharacterCount>();
        }

        protected override void OnUpdate()
        {
            var maxCharacterCount = GetSingleton<CharacterCount>().MaxValue;
            var count = m_Group.CalculateLength();
            var entityCount = maxCharacterCount - count;

            DestroyExceedingCharacters(count, maxCharacterCount, entityCount);
            InstantiateCharacters(entityCount);
        }

        private void DestroyExceedingCharacters(int count, int maxCount, int entityCount)
        {
            if (entityCount > 0) return;

            if (maxCount == 0 && count > 0)
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
                var rotationSpeed = new RotationSpeed();
                var rotationSpeedModifier = new RotationSpeedModifier();
                var walkSpeedModifier = new WalkSpeedModifier();
                var chargeSpeedModifier = new ChargeSpeedModifier();
                var engageSqrRadius = new EngageSqrRadius();
                var attackDistance = new AttackDistance { Min = 1.5f, Max = 2 };
                var attackAnimationDuration = new AttackAnimationDuration();
                var attackDamage = new AttackDamage();
                var attackSpeed = new AttackSpeed();
                var maxHealth = new MaxHealth();
                var health = new Health();
                var healthRegeneration = new HealthRegeneration();
                var viewInfo = new ViewInfo();

                PostUpdateCommands.SetComponent(entity, new Translation { Value = terrain.GetRandomPosition() });

                switch (type)
                {
                    case ViewType.Knight:
                        PostUpdateCommands.AddComponent(entity, new Knight());
                        PostUpdateCommands.AddComponent(entity, new Faction { Value = FactionType.Alliance });
                        movementSpeed.Value = m_Random.NextFloat(1, 3);
                        rotationSpeed.Value = m_Random.NextFloat(1, 3);
                        rotationSpeedModifier.Value = m_Random.NextFloat(1.25f, 2);
                        walkSpeedModifier.Value = m_Random.NextFloat(0.9f, 1.25f);
                        chargeSpeedModifier.Value = m_Random.NextFloat(2, 3);
                        engageSqrRadius.Value = m_Random.NextFloat(400, 2500);
                        attackAnimationDuration.Value = 1;
                        attackDamage.Value = m_Random.NextInt(10, 30);
                        attackSpeed.Value = m_Random.NextFloat(1, 3);
                        maxHealth.Value = m_Random.NextInt(100, 200);
                        health.Value = maxHealth.Value;
                        healthRegeneration.Value = m_Random.NextFloat(1, 3);
                        viewInfo.Type = ViewType.Knight;
                        break;

                    case ViewType.OrcWolfRider:
                        PostUpdateCommands.AddComponent(entity, new OrcWolfRider());
                        PostUpdateCommands.AddComponent(entity, new Faction { Value = FactionType.Horde });
                        movementSpeed.Value = m_Random.NextFloat(1, 3);
                        rotationSpeed.Value = m_Random.NextFloat(1, 3);
                        rotationSpeedModifier.Value = m_Random.NextFloat(1.25f, 2);
                        walkSpeedModifier.Value = m_Random.NextFloat(0.9f, 1.25f);
                        chargeSpeedModifier.Value = m_Random.NextFloat(2, 3);
                        engageSqrRadius.Value = m_Random.NextFloat(400, 2500);
                        attackAnimationDuration.Value = 1.333f;
                        attackDamage.Value = m_Random.NextInt(10, 30);
                        attackSpeed.Value = m_Random.NextFloat(1, 3);
                        maxHealth.Value = m_Random.NextInt(100, 200);
                        health.Value = maxHealth.Value;
                        healthRegeneration.Value = m_Random.NextFloat(1, 3);
                        viewInfo.Type = ViewType.OrcWolfRider;
                        break;

                    case ViewType.Skeleton:
                        PostUpdateCommands.AddComponent(entity, new Skeleton());
                        PostUpdateCommands.AddComponent(entity, new Faction { Value = FactionType.Legion });
                        movementSpeed.Value = m_Random.NextFloat(1, 3);
                        rotationSpeed.Value = m_Random.NextFloat(1, 3);
                        rotationSpeedModifier.Value = m_Random.NextFloat(1.25f, 2);
                        walkSpeedModifier.Value = m_Random.NextFloat(0.9f, 1.25f);
                        chargeSpeedModifier.Value = m_Random.NextFloat(2, 3);
                        engageSqrRadius.Value = m_Random.NextFloat(400, 2500);
                        attackAnimationDuration.Value = 2.4f;
                        attackDamage.Value = m_Random.NextInt(10, 30);
                        attackSpeed.Value = m_Random.NextFloat(1, 3);
                        maxHealth.Value = m_Random.NextInt(100, 200);
                        health.Value = maxHealth.Value;
                        healthRegeneration.Value = m_Random.NextFloat(1, 3);
                        viewInfo.Type = ViewType.Skeleton;
                        break;
                }

                PostUpdateCommands.SetComponent(entity, movementSpeed);
                PostUpdateCommands.SetComponent(entity, rotationSpeed);
                PostUpdateCommands.SetComponent(entity, rotationSpeedModifier);
                PostUpdateCommands.SetComponent(entity, walkSpeedModifier);
                PostUpdateCommands.SetComponent(entity, chargeSpeedModifier);
                PostUpdateCommands.SetComponent(entity, engageSqrRadius);
                PostUpdateCommands.SetComponent(entity, attackDistance);
                PostUpdateCommands.SetComponent(entity, attackAnimationDuration);
                PostUpdateCommands.SetComponent(entity, attackDamage);
                PostUpdateCommands.SetComponent(entity, attackSpeed);
                PostUpdateCommands.SetComponent(entity, maxHealth);
                PostUpdateCommands.SetComponent(entity, health);
                PostUpdateCommands.SetComponent(entity, healthRegeneration);
                PostUpdateCommands.SetComponent(entity, viewInfo);
#if UNITY_EDITOR
                EntityManager.SetName(entity, $"{viewInfo.Type} AI {entity}");
#endif
            }

            entityArray.Dispose();
        }
    }
}