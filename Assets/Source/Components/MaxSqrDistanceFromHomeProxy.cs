using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaxSqrDistanceFromHome : IComponentData
    {
        public float Value;
    }

    public class MaxSqrDistanceFromHomeProxy : ComponentDataProxy<MaxSqrDistanceFromHome> { }
}