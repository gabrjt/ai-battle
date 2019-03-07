using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct HealthRegeneration : IComponentData
    {
        public float Value;
    }

    public class HealthRegenerationProxy : ComponentDataProxy<HealthRegeneration> { }
}