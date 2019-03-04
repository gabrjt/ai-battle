using Game.Components;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Comparers
{
    public class TargetBufferComparer : IComparer<TargetBufferElement>
    {
        public float3 Position;

        [ReadOnly]
        public ComponentDataFromEntity<Position> PositionFromEntity;

        public int Compare(TargetBufferElement lhs, TargetBufferElement rhs)
        {
            var lhsSqrDistance = math.distancesq(PositionFromEntity[lhs.Value].Value, Position);
            var rhsSqrDistance = math.distancesq(PositionFromEntity[rhs.Value].Value, Position);

            return lhsSqrDistance.CompareTo(rhsSqrDistance);
        }
    }
}