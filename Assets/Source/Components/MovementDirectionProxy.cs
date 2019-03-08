using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct MovementDirection : IComponentData
    {
        public float3 Value;
    }

    public class MovementDirectionProxy : ComponentDataProxy<MovementDirection> { }
}