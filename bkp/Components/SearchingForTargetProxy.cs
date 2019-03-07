using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct SearchingForTarget : IComponentData
    {
        public float SqrRadius;
    }

    public class SearchingForTargetProxy : ComponentDataProxy<SearchingForTarget> { }
}