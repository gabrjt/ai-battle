using Unity.Entities;

namespace Game.Systems
{
    [UpdateAfter(typeof(DeadBarrier))]
    public class SetBarrier : BarrierSystem { }
}