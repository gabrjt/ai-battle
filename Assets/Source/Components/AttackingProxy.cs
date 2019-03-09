using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Attacking : IComponentData { }

    public class AttackingProxy : ComponentDataProxy<Attacking> { }
}