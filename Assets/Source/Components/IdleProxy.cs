using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Idle : IComponentData
    {
        public float Duration;
    }

    public class IdleProxy : ComponentDataProxy<Idle> { }
}