using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct AttackAnimationDuration : IComponentData
    {
        public float Value;
    }

    public class AttackAnimationDurationProxy : ComponentDataProxy<AttackAnimationDuration> { }
}