using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Speed : IComponentData
    {
        public float Value;
    }

    public class SpeedComponent : ComponentDataProxy<Speed> { }
}