using Unity.Entities;

namespace Game.Systems
{
    #region Instantiate

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public class InstantiateGroup : ComponentSystemGroup { }

    #endregion Instantiate

    #region Logic

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class LogicGroup : ComponentSystemGroup { }

    #endregion Logic

    #region Playback

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(LogicGroup))]
    public class PlaybackGroup : ComponentSystemGroup { }

    [UpdateInGroup(typeof(PlaybackGroup))]
    public class EventCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(PlaybackGroup))]
    [UpdateAfter(typeof(EventCommandBufferSystem))]
    public class SetCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(PlaybackGroup))]
    [UpdateAfter(typeof(SetCommandBufferSystem))]
    public class RemoveCommandBufferSystem : EntityCommandBufferSystem { }

    [UpdateInGroup(typeof(PlaybackGroup))]
    [UpdateAfter(typeof(RemoveCommandBufferSystem))]
    public class DestroyCommandBufferSystem : EntityCommandBufferSystem { }

    #endregion Playback

    #region Destroy

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(LogicGroup))]
    public class DestroyEntityGroup : ComponentSystemGroup { }

    #endregion Destroy
}