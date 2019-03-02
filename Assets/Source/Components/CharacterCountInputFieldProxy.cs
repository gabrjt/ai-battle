using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct CharacterCountInputField : IComponentData
    {
        [HideInInspector]
        public Entity Owner;
    }

    public class CharacterCountInputFieldProxy : ComponentDataProxy<CharacterCountInputField> { }
}