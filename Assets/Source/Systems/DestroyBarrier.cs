using Unity.Entities;

namespace Game.Systems
{
    [UpdateAfter(typeof(DeadBarrier))]
    [UpdateBefore(typeof(EndFrameBarrier))]
    public class DestroyBarrier : BarrierSystem { }
}