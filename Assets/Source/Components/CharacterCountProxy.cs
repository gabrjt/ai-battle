using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct CharacterCount : IComponentData
    {
        public int Value;
    }

    public class CharacterCountProxy : ComponentDataProxy<CharacterCount> { }
}