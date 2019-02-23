using Unity.Entities;

namespace Game.Components
{
    public struct Killed : IComponentData
    {
        public Entity This;

        public Entity Target;
    }

    public class KilledProxy : ComponentDataProxy<Killed> { }
}