using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct MaxDistance : IComponentData
    {
        public float3 Origin;

        public float Value;
    }

    public class MaxDistanceProxy : ComponentDataProxy<MaxDistance> { }
}