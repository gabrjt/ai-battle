using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct MaxSqrDistance : IComponentData
    {
        public float3 Origin;

        public float Value;
    }

    public class MaxSqrDistanceProxy : ComponentDataProxy<MaxSqrDistance> { }
}