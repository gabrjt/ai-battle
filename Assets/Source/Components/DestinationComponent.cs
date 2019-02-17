using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct Destination : IComponentData
    {
        public float3 Value;
    }

    public class DestinationComponent : ComponentDataWrapper<Destination>
    {
        private void OnDrawGizmosSelected()
        {
            var color = Gizmos.color;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Value.Value, 1f);
            Gizmos.color = color;
        }
    }
}