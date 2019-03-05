using Unity.Entities;

namespace Game.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class DestroyGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(DestroyGroup))]
    public class DestroyCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(DestroyGroup))]
    public class EventGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(EventGroup))]
    public class EventCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
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