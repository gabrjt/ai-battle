using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Died : IComponentData
    {
        public Entity This;
    }

    public class DiedProxy : ComponentDataProxy<Died> { }
}