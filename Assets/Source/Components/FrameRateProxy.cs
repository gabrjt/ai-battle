using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct FrameRate : IComponentData { }

    public class FrameRateProxy : ComponentDataProxy<FrameRate> { }
}