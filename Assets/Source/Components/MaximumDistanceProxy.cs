using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct MaximumDistance : IComponentData
    {
        public float3 Origin;

        public float Value;
    }

    public class MaximumDistanceProxy : ComponentDataProxy<MaximumDistance> { }
}