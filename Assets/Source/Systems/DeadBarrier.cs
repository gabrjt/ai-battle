using Unity.Entities;

namespace Game.Systems
{
    [UpdateAfter(typeof(DestroyBarrier))]
    public class DeadBarrier : BarrierSystem { }
}