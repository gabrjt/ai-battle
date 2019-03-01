using Unity.Entities;

namespace Game.Systems
{
    [UpdateAfter(typeof(EndFrameBarrier))]
    public class DeadBarrier : BarrierSystem { }
}