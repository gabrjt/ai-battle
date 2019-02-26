using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct SearchingForTarget : IComponentData
    {
        public float Interval;

        public float StartTime;

        public float Radius;
    }

    public class SearchingForTargetProxy : ComponentDataProxy<SearchingForTarget> { }
}