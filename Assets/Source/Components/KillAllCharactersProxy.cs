using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct KillAllCharacters : IComponentData { }

    public class KillAllCharactersProxy : ComponentDataProxy<KillAllCharacters> { }
}