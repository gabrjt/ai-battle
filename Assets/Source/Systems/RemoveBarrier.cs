using Unity.Entities;

namespace Game.Systems
{
    [UpdateBefore(typeof(EndFrameBarrier))]
    public class RemoveBarrier : BarrierSystem { }
}