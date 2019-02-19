using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Projectile : IComponentData
    {
        public float Radius;
    }

    public class ProjectileProxy : ComponentDataProxy<Projectile> { }
}