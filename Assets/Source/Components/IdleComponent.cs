using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Idle : IComponentData
    {
        public float IdleTime;

        public float StartTime;
    }

    public class IdleComponent : ComponentDataProxy<Idle> { }
}