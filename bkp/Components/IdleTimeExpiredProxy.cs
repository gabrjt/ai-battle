using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct IdleTimeExpired : IComponentData
    {
        public Entity This;
    }

    public class IdleTimeExpiredProxy : ComponentDataProxy<IdleTimeExpired> { }
}