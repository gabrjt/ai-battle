using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct CameraArm : IComponentData { }

    public class CameraArmProxy : ComponentDataProxy<CameraArm> { }
}