using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Owner : IComponentData
    {
        public Entity Value;
    }

    public class OwnerProxy : ComponentDataProxy<Owner> { }
}