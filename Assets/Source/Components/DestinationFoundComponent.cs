using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct DestinationFound : IComponentData
    {
        public float3 Value;
    }

    public class DestinationFoundComponent : ComponentDataWrapper<DestinationFound> { }
}