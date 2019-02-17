using Game.Components;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AI;
using MRandom = Unity.Mathematics.Random;

namespace Game.Systems
{
    public class SpawnAICharactersSystem : ComponentSystem
    {
        private GameObject m_Prefab;

        internal int m_Count = 500;

        private MRandom m_Random;

        protected override void OnCreateManager()
        {
            base.OnCreateManager();

            m_Random = new MRandom((uint)System.Environment.TickCount);

            Debug.Assert(m_Prefab = Resources.Load<GameObject>("AI Character"));
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

                PostUpdateCommands.SetComponent(entity, new Idle
                {
                    IdleTime = m_Random.NextFloat(1, 10),
                    StartTime = Time.time
                });

                PostUpdateCommands.SetComponent(entity, new SearchingForTarget
                {
                    SearchForTargetTime = 1,
                    StartTime = Time.time,
                    Radius = 5
                });
            }

            Enabled = false;
        }
    }
}