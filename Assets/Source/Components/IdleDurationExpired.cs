using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct IdleDurationExpired : IComponentData
    {
        public Entity This;
    }
}