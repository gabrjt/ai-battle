using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Comparers
{
    public class ColliderDistanceComparer : IComparer<Collider>
    {
        public float3 Position;

        public int Compare(Collider lhs, Collider rhs)
        {
            var lhsSqrDistance = math.distancesq(lhs.transform.position, Position);
            var rhsSqrDistance = math.distancesq(rhs.transform.position, Position);

            return lhsSqrDistance.CompareTo(rhsSqrDistance);
        }
    }
}