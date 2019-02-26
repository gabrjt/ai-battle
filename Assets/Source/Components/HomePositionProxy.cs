using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct HomePosition : IComponentData
    {
        public float3 Value;
    }

    public class HomePositionProxy : ComponentDataProxy<HomePosition> { }
}