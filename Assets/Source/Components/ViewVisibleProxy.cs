using System;
using Unity.Entities;

namespace Game.Systems
{
    [Serializable]
    public struct ViewVisible : IComponentData { }

    public class ViewVisibleProxy : ComponentDataProxy<ViewVisible> { }
}