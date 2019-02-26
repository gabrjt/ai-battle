using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct HealthBarOwnerPosition : IComponentData
    {
        public float3 Value;
    }

    public class HealthBarOwnerPositionProxy : ComponentDataProxy<HealthBarOwnerPosition> { }
}