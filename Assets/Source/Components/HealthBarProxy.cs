using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct HealthBar : IComponentData
    {
        [HideInInspector]
        public Entity Owner;

        public @bool Visible;
    }

    public class HealthBarProxy : ComponentDataProxy<HealthBar> { }
}