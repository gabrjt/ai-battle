using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct WalkSpeed : IComponentData
    {
        public float Value;
    }

    public class WalkSpeedProxy : ComponentDataProxy<WalkSpeed> { }
}