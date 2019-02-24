using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct CameraSingleton : IComponentData
    {
        [HideInInspector]
        public Entity Owner;
    }

    public class CameraSingletonProxy : ComponentDataProxy<CameraSingleton> { }
}