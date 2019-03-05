using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class PlaybackGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(PlaybackGroup))]
    public class EventCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(PlaybackGroup))]
    [UpdateAfter(typeof(EventCommandBufferSystem))]
    public class SetCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(PlaybackGroup))]
    [UpdateAfter(typeof(SetCommandBufferSystem))]
    public class RemoveCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class LogicGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class DestroyGroup : ComponentSystemGroup { }
}