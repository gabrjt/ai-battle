using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Dying : IComponentData
    {
        public float Duration;
    }

    public class DyingProxy : ComponentDataProxy<Dying> { }
}