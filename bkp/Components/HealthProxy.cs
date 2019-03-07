using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Health : IComponentData
    {
        public float Value;
    }

    public class HealthProxy : ComponentDataProxy<Health> { }
}