using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct AttackSpeed : IComponentData
    {
        public float Value;
    }

    public class AttackSpeedProxy : ComponentDataProxy<AttackSpeed> { }
}