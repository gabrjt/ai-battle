using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct Knight : IComponentData { }

    public class KnightProxy : ComponentDataProxy<Knight> { }
}