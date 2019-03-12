using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Walking : IComponentData { }

    public class WalkingProxy : ComponentDataProxy<Walking> { }
}