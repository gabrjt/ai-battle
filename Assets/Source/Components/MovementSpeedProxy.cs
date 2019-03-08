using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MovementSpeed : IComponentData
    {
        public float Value;
    }

    public class MovementSpeedProxy : ComponentDataProxy<MovementSpeed> { }
}