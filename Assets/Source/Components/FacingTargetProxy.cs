using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct FacingTarget : IComponentData { }

    public class FacingTargetProxy : ComponentDataProxy<FacingTarget> { }
}