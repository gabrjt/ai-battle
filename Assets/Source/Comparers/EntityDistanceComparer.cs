using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Comparers
{
    public class EntityDistanceComparer : IComparer<Entity>
    {
        public float3 Translation;

        [ReadOnly]
        public ComponentDataFromEntity<Translation> PositionFromEntity;

        public int Compare(Entity lhs, Entity rhs)
        {
            var lhsSqrDistance = math.distancesq(PositionFromEntity[lhs].Value, Translation);
            var rhsSqrDistance = math.distancesq(PositionFromEntity[rhs].Value, Translation);

            return lhsSqrDistance.CompareTo(rhsSqrDistance);
        }
    }
}