using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct AttackDistance : IComponentData
    {
        public float Minimum;

        public float Maximum;
    }

    public class AttackDistanceProxy : ComponentDataProxy<AttackDistance>
    {
    }
}