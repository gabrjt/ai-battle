using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct FrameRate : IComponentData
    {
        public @bool ShowFPS;
    }

    public class FrameRateProxy : ComponentDataProxy<FrameRate> { }
}