using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Cooldown : IComponentData
    {
        public float Duration;

        public float StartTime;
    }

    public class CooldownProxy : ComponentDataProxy<Cooldown> { }
}