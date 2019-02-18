using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Damage : IComponentData
    {
        public float Value;
    }

    public class DamageProxy : ComponentDataProxy<Damage> { }
}