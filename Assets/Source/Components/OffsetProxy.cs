using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct Offset : IComponentData
    {
        public float3 Value;
    }

    public class OffsetProxy : ComponentDataProxy<Offset> { }
}