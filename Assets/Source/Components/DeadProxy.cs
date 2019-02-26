using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Dead : IComponentData
    {
        public float Duration;

        public float StartTime;
    }

    public class DeadProxy : ComponentDataProxy<Dead> { }
}