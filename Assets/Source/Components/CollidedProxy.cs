using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Collided : IComponentData { }

    public class CollidedProxy : ComponentDataProxy<Collided> { }
}