using Game.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Comparers
{
    public class TargetBufferComparer : IComparer<TargetBuffer>
    {
        public float3 Translation;

        [ReadOnly]
        public ComponentDataFromEntity<Translation> PositionFromEntity;

        public int Compare(TargetBuffer lhs, TargetBuffer rhs)
        {
            var lhsSqrDistance = math.distancesq(PositionFromEntity[lhs.Value].Value, Translation);
            var rhsSqrDistance = math.distancesq(PositionFromEntity[rhs.Value].Value, Translation);

            return lhsSqrDistance.CompareTo(rhsSqrDistance);
        }
    }
}