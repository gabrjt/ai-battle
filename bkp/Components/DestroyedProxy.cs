using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Destroyed : IComponentData
    {
        public Entity This;
    }

    public class DestroyedProxy : ComponentDataProxy<Destroyed> { }
}