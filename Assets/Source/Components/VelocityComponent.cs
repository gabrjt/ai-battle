using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct Velocity : IComponentData
    {
        public float3 Value;
    }

    public class VelocityComponent : ComponentDataWrapper<Velocity> { }
}