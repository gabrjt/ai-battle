using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct TargetInRange : IComponentData { }

    public class TargetInRangeProxy : ComponentDataProxy<TargetInRange> { }
}