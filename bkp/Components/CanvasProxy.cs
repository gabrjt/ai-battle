using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct Canvas : IComponentData    {    }

    public class CanvasProxy : ComponentDataProxy<Canvas> { }
}