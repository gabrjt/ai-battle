using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Damaged : IComponentData
    {
        public float Value;

        public Entity This;

        public Entity Other;
    }

    public class DamagedProxy : ComponentDataProxy<Damaged> { }
}