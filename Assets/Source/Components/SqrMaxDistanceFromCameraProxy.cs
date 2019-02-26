using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct SqrMaxDistanceFromCamera : IComponentData
    {
        public float Value;
    }

    public class SqrMaxDistanceFromCameraProxy : ComponentDataProxy<SqrMaxDistanceFromCamera> { }
}