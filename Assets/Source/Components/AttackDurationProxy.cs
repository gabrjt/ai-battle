using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct AttackDuration : IComponentData
    {
        public float Value;
    }

    public class AttackDurationProxy : ComponentDataProxy<AttackAnimationDuration> { }
}