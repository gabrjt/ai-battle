using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Damaged : IComponentData
    {
        public Entity This;
        public Entity Other;
        public float Value;
    }

    public class DamagedProxy : ComponentDataProxy<Damaged> { }
}