using System;
using Unity.Entities;

namespace Game.Components
{
    [Serializable]
    public struct Group : IComponentData
    {
        public int Value;
    }

    public class GroupProxy : ComponentDataProxy<Group> { }
}