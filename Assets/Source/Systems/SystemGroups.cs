using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class EntityLifecycleGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EntityLifecycleGroup))]
    public class GameLogicGroup : ComponentSystemGroup { }
}