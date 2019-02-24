using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct CanvasSingleton : IComponentData
    {
        [HideInInspector]
        public @bool Initialized;
    }

    public class CanvasSingletonProxy : ComponentDataProxy<CanvasSingleton> { }
}