using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Visible : IComponentData { }

    public class VisibleProxy : ComponentDataProxy<Visible> { }
}