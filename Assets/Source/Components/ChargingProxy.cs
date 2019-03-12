using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Charging : IComponentData { }

    public class ChargingProxy : ComponentDataProxy<Charging> { }
}