using Unity.Entities;

namespace Game.Systems
{
    [UpdateAfter(typeof(SetBarrier))]
    public class RemoveBarrier : BarrierSystem { }
}