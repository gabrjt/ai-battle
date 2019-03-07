using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Attacked : IComponentData
    {
        public Entity This;
    }

    public class AttackedProxy : ComponentDataProxy<Attacked> { }
}