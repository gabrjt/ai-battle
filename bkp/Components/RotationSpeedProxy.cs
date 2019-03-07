using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct RotationSpeed : IComponentData
    {
        public float Value;
    }

    public class RotationSpeedProxy : ComponentDataProxy<RotationSpeed> { }
}