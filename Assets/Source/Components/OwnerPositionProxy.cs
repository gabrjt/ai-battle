using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct OwnerPosition : IComponentData
    {
        public float3 Value;
    }

    public class OwnerPositionProxy : ComponentDataProxy<OwnerPosition> { }
}