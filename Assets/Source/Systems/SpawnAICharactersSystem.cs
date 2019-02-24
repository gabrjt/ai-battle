using Game.Components;
using Game.Extensions;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    public class SpawnAICharactersSystem : ComponentSystem
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
            Debug.Assert(m_ViewPrefab = Resources.Load<GameObject>("Orc Wolf Rider"));
        }

        protected override void OnUpdate()
        {
            var terrain = Terrain.activeTerrain;

            for (var i = 0; i < m_Count; i++)
            {
                var navMeshAgent = Object.Instantiate(m_Prefab).GetComponent<NavMeshAgent>();
                navMeshAgent.Warp(terrain.GetRandomPosition());
                navMeshAgent.transform.rotation = m_Random.NextQuaternionRotation();

                var entity = navMeshAgent.GetComponent<GameObjectEntity>().Entity;

                var maximumHealth = m_Random.NextInt(20, 100);
                PostUpdateCommands.SetComponent(entity, new MaximumHealth { Value = maximumHealth });
                PostUpdateCommands.SetComponent(entity, new Health { Value = maximumHealth });

                var view = Object.Instantiate(m_ViewPrefab).GetComponent<GameObjectEntity>().Entity;
                PostUpdateCommands.SetComponent(view, new View
                {
                    Owner = entity,
                    Offset = new float3(0, -1, 0)
                });
                /*
                var attach = PostUpdateCommands.CreateEntity(m_Archetype);
                PostUpdateCommands.SetComponent(attach, new Attach
                {
                    Parent = entity,
                    Child = view
                });
                */
            }

            Enabled = false;
        }
    }
}