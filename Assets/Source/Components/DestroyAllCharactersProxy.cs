using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct DestroyAllCharacters : IComponentData { }

    public class DestroyAllCharactersProxy : ComponentDataProxy<DestroyAllCharacters> { }
}