using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct WalkSpeedModifier : IComponentData
    {
        public float Value;
    }

    public class WalkSpeedModifierProxy : ComponentDataProxy<WalkSpeedModifier> { }
}