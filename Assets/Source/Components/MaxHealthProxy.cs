using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaxHealth : IComponentData
    {
        public float Value;
    }

    public class MaxHealthProxy : ComponentDataProxy<MaxHealth> { }
}