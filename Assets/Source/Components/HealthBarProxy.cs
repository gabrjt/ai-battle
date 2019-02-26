using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct HealthBar : IComponentData
    {
        public @bool IsVisible;

        public float MaxSqrDistanceFromCamera;
    }

    public class HealthBarProxy : ComponentDataProxy<HealthBar> { }
}