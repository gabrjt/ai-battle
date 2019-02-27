using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaxSqrDistanceFromCamera : IComponentData
    {
        public float Value;
    }

    public class MaxSqrDistanceFromCameraProxy : ComponentDataProxy<MaxSqrDistanceFromCamera> { }
}