using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct PoolableObjectCount : IComponentData
    {
        public int Value;
    }

    public class PoolableObjectCountProxy : ComponentDataProxy<PoolableObjectCount> { }
}