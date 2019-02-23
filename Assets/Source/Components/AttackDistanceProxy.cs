﻿using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct AttackDistance : IComponentData
    {
        public float Value;
    }

    public class AttackDistanceProxy : ComponentDataProxy<AttackDistance>
    {
    }
}