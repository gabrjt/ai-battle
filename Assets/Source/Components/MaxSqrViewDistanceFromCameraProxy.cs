using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaxSqrViewDistanceFromCamera : IComponentData
    {
        public float Value;
    }

    public class MaxSqrViewDistanceFromCameraProxy : ComponentDataProxy<MaxSqrViewDistanceFromCamera> { }
}