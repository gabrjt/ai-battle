using System;
using UnityEngine;

namespace Game.Components
{
    [Serializable]
    public struct @bool : IComparable, IComparable<@bool>, IComparable<bool>, IEquatable<@bool>, IEquatable<bool>
    {
        public static @bool False => new @bool(0x00);

        public static @bool True => new @bool(0xFF);

        [SerializeField]
        internal byte m_Value;

        private @bool(byte value) => m_Value = value;

        public override bool Equals(object obj) => base.Equals((@bool)obj);

        public override int GetHashCode() => ((bool)this).GetHashCode();

        public int CompareTo(object obj) => CompareTo((@bool)obj);

        public int CompareTo(@bool other) => CompareTo((bool)other);

        public int CompareTo(bool other) => ((bool)this).CompareTo(other);

        public bool Equals(@bool other) => this == other;

        public bool Equals(bool other) => this == other;

        public static implicit operator bool(@bool b) => b.m_Value != 0x00;

        public static implicit operator @bool(bool b) => b ? True : False;

        public static bool operator ==(@bool lhs, @bool rhs) => (bool)lhs == (bool)rhs;

        public static bool operator !=(@bool lhs, @bool rhs) => (bool)lhs != (bool)rhs;

        public override string ToString() { return (this == true).ToString(); }
    }
}