using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct RotationSpeedModifier : IComponentData
    {
        public float Value;
    }

    public class RotationSpeedModifierProxy : ComponentDataProxy<RotationSpeedModifier> { }
}