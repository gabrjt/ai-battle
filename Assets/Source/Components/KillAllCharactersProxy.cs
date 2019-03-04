using System;
using Unity.Entities;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct KillAllCharacters : IComponentData
    {
        [HideInInspector]
        public @bool Cachorrada;
    }

    public class KillAllCharactersProxy : ComponentDataProxy<KillAllCharacters> { }
}