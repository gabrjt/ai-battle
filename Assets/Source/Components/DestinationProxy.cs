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

        public float3 LastValue;

        public bool IsDirty
        {
            get
            {
                var equals = Value == LastValue;
                return !(equals.x && equals.y && equals.z);
            }
        }
    }

    public class DestinationProxy : ComponentDataProxy<Destination>
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