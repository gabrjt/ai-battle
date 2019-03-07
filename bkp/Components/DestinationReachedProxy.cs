using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct DestinationReached : IComponentData
    {
        public Entity This;
    }

    public class DestinationReachedProxy : ComponentDataProxy<DestinationReached> { }
}