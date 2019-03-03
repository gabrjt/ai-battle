using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct CameraSingleton : IComponentData
    {
        public Entity Owner;
    }

    public class CameraSingletonProxy : ComponentDataProxy<CameraSingleton> { }
}