using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Game.Comparers
{
    public struct EntityDistanceComparer : IComparer<Entity>
    {
        public float3 Translation;

        [ReadOnly]
        public ComponentDataFromEntity<Translation> TranslationFromEntity;

        public int Compare(Entity lhs, Entity rhs)
        {
            var lhsSqrDistance = math.distancesq(TranslationFromEntity[lhs].Value, Translation);
            var rhsSqrDistance = math.distancesq(TranslationFromEntity[rhs].Value, Translation);

            return lhsSqrDistance.CompareTo(rhsSqrDistance);
        }
    }
}