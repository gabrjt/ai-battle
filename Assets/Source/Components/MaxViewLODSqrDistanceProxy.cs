using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaxViewLODSqrDistance : IComponentData
    {
        public float Value;
    }

    public class MaxViewLODSqrDistanceProxy : ComponentDataProxy<MaxViewLODSqrDistance> { }
}