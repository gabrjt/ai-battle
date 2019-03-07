using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Attack : IComponentData
    {
        public float Value;
    }

    public class AttackProxy : ComponentDataProxy<Attack> { }
}