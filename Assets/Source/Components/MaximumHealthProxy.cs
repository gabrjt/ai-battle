﻿using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct MaximumHealth : IComponentData
    {
        public float Value;
    }

    public class MaximumHealthProxy : ComponentDataProxy<MaximumHealth> { }
}