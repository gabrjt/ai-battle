using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct PoolableObjectCount : IComponentData
    {
        [HideInInspector]
        public int Value;
    }

    public class PoolableObjectCountProxy : ComponentDataProxy<PoolableObjectCount> { }
}