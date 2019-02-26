using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Visible : IComponentData
    {
        public @bool Value;

        public @bool LastValue;
    }

    public class VisibleProxy : ComponentDataProxy<Visible> { }
}