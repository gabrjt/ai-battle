using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct CharacterCountInputField : IComponentData { }

    public class CharacterCountInputFieldProxy : ComponentDataProxy<CharacterCountInputField> { }
}