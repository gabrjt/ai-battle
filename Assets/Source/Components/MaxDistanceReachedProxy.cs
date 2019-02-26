using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaxDistanceReached : IComponentData
    {
        public Entity This;
    }

    public class MaxDistanceReachedProxy : ComponentDataProxy<MaxDistanceReached> { }
}