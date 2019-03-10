using Game.Enums;
using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct ViewInfo : IComponentData
    {
        [HideInInspector]
        public ViewType Type;
    }

    public class ViewInfoProxy : ComponentDataProxy<ViewInfo> { }
}