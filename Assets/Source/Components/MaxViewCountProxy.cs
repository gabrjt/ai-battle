using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaxViewCount : IComponentData
    {
        public int Value;
    }

    public class MaxViewCountProxy : ComponentDataProxy<MaxViewCount> { }
}