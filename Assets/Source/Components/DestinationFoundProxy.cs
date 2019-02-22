using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct DestinationFound : IComponentData
    {
        public Entity This;

        public float3 Value;
    }

    public class DestinationFoundProxy : ComponentDataProxy<DestinationFound> { }
}