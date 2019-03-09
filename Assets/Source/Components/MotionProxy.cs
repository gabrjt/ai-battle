using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct Motion : IComponentData
    {
        public float3 Value;
    }

    public class MotionProxy : ComponentDataProxy<Motion> { }
}