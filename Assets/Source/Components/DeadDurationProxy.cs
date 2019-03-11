using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct DeadDuration : IComponentData
    {
        public float Value;
    }

    public class DeadDurationProxy : ComponentDataProxy<DeadDuration> { }
}