using Unity.Entities;

namespace Game.Components
{
    public struct Killed : IComponentData
    {
        public Entity This;
        public Entity Other;
    }

    public class KilledProxy : ComponentDataProxy<Killed> { }
}