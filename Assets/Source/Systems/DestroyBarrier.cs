using Unity.Entities;

namespace Game.Systems
{
    [UpdateAfter(typeof(EventBarrier))]
    public class DestroyBarrier : BarrierSystem { }
}