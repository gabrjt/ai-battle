using Game.Enums;
using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct View : IComponentData
    {
        public ViewType Value;
    }

    public class ViewProxy : ComponentDataProxy<View> { }
}