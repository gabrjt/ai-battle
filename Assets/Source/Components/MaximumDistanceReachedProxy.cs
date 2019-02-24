using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaximumDistanceReached : IComponentData
    {
        public Entity This;
    }

    public class MaximumDistanceReachedProxy : ComponentDataProxy<MaximumDistanceReached> { }
}