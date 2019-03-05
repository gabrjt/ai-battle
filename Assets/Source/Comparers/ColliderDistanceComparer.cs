using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Comparers
{
    public class ColliderDistanceComparer : IComparer<Collider>
    {
        public float3 Translation;

        public int Compare(Collider lhs, Collider rhs)
        {
            var lhsSqrDistance = math.distancesq(lhs.transform.position, Translation);
            var rhsSqrDistance = math.distancesq(rhs.transform.position, Translation);

            return lhsSqrDistance.CompareTo(rhsSqrDistance);
        }
    }
}