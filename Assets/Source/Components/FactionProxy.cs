using Game.Enums;
using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Faction : IComponentData
    {
        public FactionType Value;
    }

    public class FactionProxy : ComponentDataProxy<Faction> { }
}