using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct CameraArm : IComponentData
    {
        [HideInInspector]
        public @bool ZeroSized;
    }

    public class CameraArmProxy : ComponentDataProxy<CameraArm> { }
}