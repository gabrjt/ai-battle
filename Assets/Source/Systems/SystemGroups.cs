using Unity.Entities;
using Unity.Transforms;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class EntityLifecycleGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(EntityLifecycleGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public class GameLogicGroup : ComponentSystemGroup { }
}