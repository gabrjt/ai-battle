using Unity.Entities;

namespace Game.Systems
{
    [UpdateBefore(typeof(RemoveBarrier))]
    public class SetBarrier : BarrierSystem { }
}