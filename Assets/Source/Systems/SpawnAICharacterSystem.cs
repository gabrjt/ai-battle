using Game.Components;
using Game.Extensions;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    public class SpawnAICharacterSystem : ComponentSystem
    {
        private EntityArchetype m_Archetype;

        private GameObject m_Prefab;

        private GameObject m_ViewPrefab;

        internal int m_Count = 100;

        private MRandom m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Archetype = EntityManager.CreateArchetype(ComponentType.ReadOnly<Attach>());

            m_Random = new MRandom((uint)System.Environment.TickCount);

            Debug.Assert(m_Prefab = Resources.Load<GameObject>("AI Character View"));
        }

        protected override void OnUpdate()
        {
            var terrain = Terrain.activeTerrain;

            for (var i = 0; i < m_Count; i++)
            {
                var navMeshAgent = Object.Instantiate(m_Prefab).GetComponent<NavMeshAgent>();
                navMeshAgent.Warp(terrain.GetRandomPosition());
                navMeshAgent.transform.rotation = m_Random.NextQuaternionRotation();
                // navMeshAgent.updateRotation = false;

                var entity = navMeshAgent.GetComponent<GameObjectEntity>().Entity;

                var maximumHealth = m_Random.NextInt(20, 100);
                PostUpdateCommands.SetComponent(entity, new MaximumHealth { Value = maximumHealth });
                PostUpdateCommands.SetComponent(entity, new Health { Value = maximumHealth });
            }

            Enabled = false;
        }
    }
}