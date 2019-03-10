using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct View : IComponentData { }

    public class ViewProxy : ComponentDataProxy<View> { }
}