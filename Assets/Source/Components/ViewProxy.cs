using System;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Components
{
    [Serializable]
    public struct View : IComponentData
    {
        public Entity Owner;

        public float3 Offset;
    }

    public class ViewProxy : ComponentDataProxy<View> { }
}