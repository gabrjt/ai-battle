using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct IdleDuration : IComponentData
    {
        public float Value;
    }

    public class IdleDurationProxy : ComponentDataProxy<IdleDuration> { }
}