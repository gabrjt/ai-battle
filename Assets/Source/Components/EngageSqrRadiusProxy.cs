using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct EngageSqrRadius : IComponentData
    {
        public float Value;
    }

    public class EngageSqrRadiusProxy : ComponentDataProxy<EngageSqrRadius> { }
}