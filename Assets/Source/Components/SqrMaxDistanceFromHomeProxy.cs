using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct SqrMaxDistanceFromHome : IComponentData
    {
        public float Value;
    }

    public class SqrMaxDistanceFromHomeProxy : ComponentDataProxy<SqrMaxDistanceFromHome> { }
}