using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct ChargeSpeedModifier : IComponentData
    {
        public float Value;
    }

    public class ChargeSpeedModifierProxy : ComponentDataProxy<ChargeSpeedModifier> { }
}