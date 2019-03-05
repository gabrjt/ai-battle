using Game.Enums;
using System.Collections.Generic;

namespace Game.Comparers
{
    public struct CharacterCountSortData
    {
        public ViewType ViewType;

        public int Count;
    }

    public class CharacterCountComparer : IComparer<CharacterCountSortData>
    {
        public int Compare(CharacterCountSortData lhs, CharacterCountSortData rhs)
        {
            return lhs.Count.CompareTo(rhs.Count);
        }
    }
}