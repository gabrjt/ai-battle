using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct SearchingForDestination : IComponentData { }

    public class SearchingForDestinationComponent : ComponentDataWrapper<SearchingForDestination> { }
}