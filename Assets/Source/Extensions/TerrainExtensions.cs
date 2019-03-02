using Unity.Mathematics;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Game.Extensions
{
    public static class TerrainExtensions
    {
        private static Random m_Random = new Random((uint)System.Environment.TickCount);

        public static float3 GetRandomPosition(this Terrain terrain)
        {
            var terrainWidth = (int)terrain.terrainData.size.x;
            var terrainLength = (int)terrain.terrainData.size.z;
            var terrainPositionX = (int)terrain.transform.position.x;
            var terrainPositionZ = (int)terrain.transform.position.z;

            var positionX = m_Random.NextFloat(terrainPositionX, terrainPositionX + terrainWidth);
            var positionZ = m_Random.NextFloat(terrainPositionZ, terrainPositionZ + terrainLength);
            var positionY = terrain.SampleHeight(new float3(positionX, 0, positionZ));

            return new float3(positionX, positionY, positionZ);
        }
    }
}