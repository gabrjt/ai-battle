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
        internal int m_TotalCount = 0xFFF;
        internal int m_LastTotalCount;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() },
                None = new[] { ComponentType.ReadOnly<Destroy>() }
            });

            m_Archetype = EntityManager.CreateArchetype(
                ComponentType.ReadWrite<Character>(),
                ComponentType.ReadWrite<Translation>(),
                ComponentType.ReadWrite<Rotation>(),
                ComponentType.ReadWrite<MovementSpeed>(),
                ComponentType.ReadWrite<EngageSqrRadius>(),
                ComponentType.ReadWrite<AttackDistance>()
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

                for (var entityIndex = 0; entityIndex < entityCount; entityIndex++)
                {
                    PostUpdateCommands.AddComponent(entityArray[entityIndex], new Destroy());
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

                PostUpdateCommands.SetComponent(entity, new Translation { Value = terrain.GetRandomPosition() });
                PostUpdateCommands.SetComponent(entity, new MovementSpeed { Value = m_Random.NextFloat(1, 3) });
                PostUpdateCommands.SetComponent(entity, new EngageSqrRadius { Value = m_Random.NextFloat(25, 225) });
                PostUpdateCommands.SetComponent(entity, new AttackDistance { Min = 1.2f, Max = 1.5f });

                switch (type)
                {
                    case ViewType.Knight:
                        PostUpdateCommands.AddComponent(entity, new Knight());
                        break;

                    case ViewType.OrcWolfRider:
                        PostUpdateCommands.AddComponent(entity, new OrcWolfRider());
                        break;

                    case ViewType.Skeleton:
                        PostUpdateCommands.AddComponent(entity, new Skeleton());
                        break;
                }
            }

            entityArray.Dispose();
        }
    }
}