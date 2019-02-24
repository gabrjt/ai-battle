using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Character : IComponentData { }

    public class CharacterProxy : ComponentDataProxy<Character> { }
}