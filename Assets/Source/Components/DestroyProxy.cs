using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Destroy : IComponentData { }

    public class DestroyProxy : ComponentDataProxy<Destroy> { }
}