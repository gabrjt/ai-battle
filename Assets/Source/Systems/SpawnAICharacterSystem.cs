using Game.Components;
using Game.Extensions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    [AlwaysUpdateSystem]
    public class SpawnAICharacterSystem : ComponentSystem
    {
        private ComponentGroup m_Group;

        private EntityArchetype m_Archetype;

        private GameObject m_Prefab;

        private GameObject m_ViewPrefab;

        private int m_TotalCount = 50;

        private MRandom m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Group = GetComponentGroup(new EntityArchetypeQuery
            {
                All = new[] { ComponentType.ReadOnly<Character>() }
            });

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Attach>());

            m_Random = new MRandom((uint)System.Environment.TickCount);

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
            for (var i = count; i < m_TotalCount; i++)
            {
                var navMeshAgent = Object.Instantiate(m_Prefab).GetComponent<NavMeshAgent>();
                navMeshAgent.Warp(terrain.GetRandomPosition());
                navMeshAgent.transform.rotation = m_Random.NextQuaternionRotation();

                var entity = navMeshAgent.GetComponent<GameObjectEntity>().Entity;

                navMeshAgent.name = $"Character AI {entity.Index}";

                PostUpdateCommands.SetComponent(entity, new HomePosition { Value = navMeshAgent.transform.position });

                PostUpdateCommands.SetComponent(entity, new Position { Value = navMeshAgent.transform.position });
                PostUpdateCommands.SetComponent(entity, new Rotation { Value = navMeshAgent.transform.rotation });

                var MaxHealth = m_Random.NextInt(100, 301);
                PostUpdateCommands.SetComponent(entity, new MaxHealth { Value = MaxHealth });
                PostUpdateCommands.SetComponent(entity, new Health { Value = MaxHealth });

                PostUpdateCommands.SetComponent(entity, new Attack { Value = m_Random.NextInt(10, 31) });

                PostUpdateCommands.SetComponent(entity, new AttackSpeed { Value = m_Random.NextFloat(1, 4) });

                PostUpdateCommands.SetComponent(entity, new HealthRegeneration { Value = m_Random.NextFloat(1, 6) });

                var viewIndex = m_Random.NextInt(0, 3);

                switch (viewIndex)
                {
                    case 0:
                        PostUpdateCommands.AddComponent(entity, new Knight());
                        PostUpdateCommands.SetComponent(entity, new AttackDuration { Value = 1 });
                        break;

                    case 1:
                        PostUpdateCommands.AddComponent(entity, new OrcWolfRider());
                        PostUpdateCommands.SetComponent(entity, new AttackDuration { Value = 1.333f });
                        break;

                    case 2:
                        PostUpdateCommands.AddComponent(entity, new Skeleton());
                        PostUpdateCommands.SetComponent(entity, new AttackDuration { Value = 2.4f });
                        break;

                    default:
                        break;
                }
            }
        }
    }
}