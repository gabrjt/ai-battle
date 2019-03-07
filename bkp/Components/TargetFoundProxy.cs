using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct TargetFound : IComponentData
    {
        public Entity This;

        public Entity Other;
    }

    public class TargetFoundProxy : ComponentDataProxy<TargetFound> { }
}