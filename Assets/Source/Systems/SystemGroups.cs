using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class EntityLifecycleGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EntityLifecycleGroup))]
    public class CleanupGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(CleanupGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class GameLogicGroup : ComponentSystemGroup { }
}