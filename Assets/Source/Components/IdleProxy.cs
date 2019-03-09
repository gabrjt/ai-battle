using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Idle : IComponentData { }

    public class IdleProxy : ComponentDataProxy<Idle> { }
}