using Unity.Entities;

namespace Game.Systems
{
    [UpdateBefore(typeof(SetBarrier))]
    public class EventBarrier : BarrierSystem { }
}