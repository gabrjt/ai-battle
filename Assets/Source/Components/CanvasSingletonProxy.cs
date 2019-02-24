﻿using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct CanvasSingleton : IComponentData
    {
        [HideInInspector]
        public Entity Owner;
    }

    public class CanvasSingletonProxy : ComponentDataProxy<CanvasSingleton> { }
}