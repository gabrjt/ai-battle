using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Event : IComponentData { }

    public class EventProxy : ComponentDataProxy<Event> { }
}