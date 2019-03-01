using Unity.Entities;

namespace Game.Systems
{
    [UpdateBefore(typeof(RemoveBarrier))]
    [UpdateAfter(typeof(SetBarrier))]
    public class DeadBarrier : BarrierSystem { }
}