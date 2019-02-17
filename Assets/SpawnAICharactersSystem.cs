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
            // var entities = new NativeArray<Entity>(m_Count, Allocator.Temp);

            // EntityManager.Instantiate(m_Prefab, entities);

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

                PostUpdateCommands.SetComponent(entity, new SearchForTarget
                {
                    SearchForTargetTime = 1,
                    StartTime = Time.time,
                    Radius = 5
                });
            }

            /*
            for (var i = 0; i < entities.Length; i++)
            {
                var positionX = m_Random.NextFloat(terrainPositionX, terrainPositionX + terrainWidth);
                var positionZ = m_Random.NextFloat(terrainPositionZ, terrainPositionZ + terrainLength);
                var positionY = terrain.SampleHeight(new float3(positionX, 0, positionZ));

                var navMeshAgent = EntityManager.GetComponentObject<NavMeshAgent>(entities[i]);
                navMeshAgent.Warp(new float3(positionX, positionY, positionZ));

                PostUpdateCommands.SetComponent(entities[i], new Position { Value = new float3(positionX, positionY, positionZ) });
            }
            */

            // entities.Dispose();

            Enabled = false;
        }
    }
}