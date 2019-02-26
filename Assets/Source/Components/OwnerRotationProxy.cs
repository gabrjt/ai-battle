using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct OwnerRotation : IComponentData
    {
        public quaternion Value;
    }

    public class OwnerRotationProxy : ComponentDataProxy<OwnerRotation> { }
}