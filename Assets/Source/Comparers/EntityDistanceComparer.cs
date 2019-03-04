using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Comparers
{
    public class EntityDistanceComparer : IComparer<Entity>
    {
        public float3 Position;

        [ReadOnly]
        public ComponentDataFromEntity<Position> PositionFromEntity;

        public int Compare(Entity lhs, Entity rhs)
        {
            var lhsSqrDistance = math.distancesq(PositionFromEntity[lhs].Value, Position);
            var rhsSqrDistance = math.distancesq(PositionFromEntity[rhs].Value, Position);

            return lhsSqrDistance.CompareTo(rhsSqrDistance);
        }
    }
}