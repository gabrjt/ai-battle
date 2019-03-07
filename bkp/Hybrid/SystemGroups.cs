using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(BeginInitializationEntityCommandBufferSystem))]
    public class DestroyGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(DestroyGroup))]
    public class DestroyCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
    public class EventGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(EventGroup))]
    public class EventCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(EventGroup))]
    public class DeadGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(DeadGroup))]
    public class DeadCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(DeadGroup))]
    public class SetGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(SetGroup))]
    public class SetCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    [UpdateAfter(typeof(SetGroup))]
    public class RemoveGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(RemoveGroup))]
    public class RemoveCommandBufferSystem : EntityCommandBufferSystem { }
}