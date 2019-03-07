using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Skeleton : IComponentData { }

    public class SkeletonProxy : ComponentDataProxy<Skeleton> { }
}