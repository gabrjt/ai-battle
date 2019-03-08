using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct AttackDamage : IComponentData
    {
        public float Value;
    }

    public class AttackDamageProxy : ComponentDataProxy<AttackDamage> { }
}