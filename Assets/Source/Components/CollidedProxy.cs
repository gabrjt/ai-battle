using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Collided : IComponentData
    {
        public Entity This;

        public Entity Other;
    }

    public class CollidedProxy : ComponentDataProxy<Collided> { }
}