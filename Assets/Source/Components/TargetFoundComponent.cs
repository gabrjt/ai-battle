using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct TargetFound : IComponentData
    {
        public Entity Value;
    }

    public class TargetFoundComponent : ComponentDataWrapper<TargetFound> { }
}