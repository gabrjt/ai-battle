using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct SpectatorCamera : IComponentData
    {
        public float RotationSpeed;
    }

    public class SpectatorCameraProxy : ComponentDataProxy<SpectatorCamera> { }
}