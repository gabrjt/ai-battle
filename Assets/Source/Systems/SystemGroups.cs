using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class EntityLifecycleGroup : ComponentSystemGroup { }
}