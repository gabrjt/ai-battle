using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct ViewReference : IComponentData
    {
        public Entity Value;
    }

    public class ViewReferenceProxy : ComponentDataProxy<ViewReference> { }
}