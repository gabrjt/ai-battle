using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Target : IComponentData
    {
        public Entity Value;
    }

    public class TargetComponent : ComponentDataProxy<Target> { }
}