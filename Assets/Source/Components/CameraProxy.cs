using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Camera : IComponentData { }

    public class CameraProxy : ComponentDataProxy<Camera> { }
}