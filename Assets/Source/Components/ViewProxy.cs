using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct View : IComponentData
    {
        public @bool IsVisible;

        public float MaxSqrDistanceFromCamera;
    }

    public class ViewProxy : ComponentDataProxy<View> { }
}