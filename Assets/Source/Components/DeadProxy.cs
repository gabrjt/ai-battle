using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Dead : IComponentData { }

    public class DeadProxy : ComponentDataProxy<Dead> { }
}