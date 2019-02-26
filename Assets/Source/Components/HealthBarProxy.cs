using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct HealthBar : IComponentData { }

    public class HealthBarProxy : ComponentDataProxy<HealthBar> { }
}