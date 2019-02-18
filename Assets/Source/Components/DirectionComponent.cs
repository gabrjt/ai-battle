using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct Direction : IComponentData
    {
        public float3 Value;
    }

    public class DirectionComponent : ComponentDataProxy<Direction> { }
}