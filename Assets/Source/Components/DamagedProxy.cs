using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Damaged : IComponentData
    {
        public float Value;

        public Entity Source;

        public Entity Target;
    }

    public class DamagedProxy : ComponentDataProxy<Damaged> { }
}