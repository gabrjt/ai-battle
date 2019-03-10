using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct SpectatorCamera : IComponentData
    {
        public Entity Target;
        public float RotationSpeed;
    }

    public class SpectatorCameraProxy : ComponentDataProxy<SpectatorCamera> { }
}