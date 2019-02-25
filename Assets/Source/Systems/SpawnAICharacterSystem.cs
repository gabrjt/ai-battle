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

        private int m_TotalCount = 1000;

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

            Application.targetFrameRate = 0;
            QualitySettings.vSyncCount = 0;
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
                // navMeshAgent.updateRotation = false;

                var entity = navMeshAgent.GetComponent<GameObjectEntity>().Entity;

                var maximumHealth = m_Random.NextInt(100, 301);
                PostUpdateCommands.SetComponent(entity, new MaximumHealth { Value = maximumHealth });
                PostUpdateCommands.SetComponent(entity, new Health { Value = maximumHealth });

                PostUpdateCommands.SetComponent(entity, new AttackSpeed { Value = m_Random.NextInt(1, 4) });

                PostUpdateCommands.SetComponent(entity, new HealthRegeneration { Value = m_Random.NextInt(1, 6) });

                var viewIndex = m_Random.NextInt(0, 3);

                switch (viewIndex)
                {
                    case 0:
                        PostUpdateCommands.AddComponent(entity, new Knight());
                        break;

                    case 1:
                        PostUpdateCommands.AddComponent(entity, new OrcWolfRider());
                        break;

                    case 2:
                        PostUpdateCommands.AddComponent(entity, new Skeleton());
                        break;

                    default:
                        break;
                }
            }
        }
    }
}