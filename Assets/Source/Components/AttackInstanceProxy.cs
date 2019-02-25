using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct AttackInstance : IComponentData
    {
        public Entity Owner;

        public float Radius;
    }

    public class AttackInstanceProxy : ComponentDataProxy<AttackInstance> { }
}