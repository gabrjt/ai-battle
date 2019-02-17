using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct SearchForTarget : IComponentData
    {
        public float SearchForTargetTime;

        public float StartTime;

        public float Radius;
    }

    public class SearchForTargetComponent : ComponentDataWrapper<SearchForTarget> { }
}