using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaxSqrViewDistanceFromCamera : ISharedComponentData
    {
        public float Value;
    }
}