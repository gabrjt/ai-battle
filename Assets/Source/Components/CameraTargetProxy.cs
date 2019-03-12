using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct CameraTarget : IComponentData
    {
        public Entity Value;
    }

    public class CameraTargetProxy : ComponentDataProxy<CameraTarget> { }
}