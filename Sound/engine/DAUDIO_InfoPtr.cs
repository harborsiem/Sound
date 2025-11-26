using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SystemX.Media.Sound {
    [DebuggerDisplay("{Value}")]
    internal unsafe readonly partial struct DAUDIO_InfoPtr : IEquatable<DAUDIO_InfoPtr> {
        internal readonly void* Value;

        internal DAUDIO_InfoPtr(void* value) => this.Value = value;

        internal DAUDIO_InfoPtr(UIntPtr value) : this((void*)value) {
        }

        internal static DAUDIO_InfoPtr Null => default;

        internal bool IsNull => Value == default;

        public static implicit operator void*(DAUDIO_InfoPtr value) => value.Value;

        public static explicit operator DAUDIO_InfoPtr(void* value) => new DAUDIO_InfoPtr(value);

        public static bool operator ==(DAUDIO_InfoPtr left, DAUDIO_InfoPtr right) => left.Value == right.Value;

        public static bool operator !=(DAUDIO_InfoPtr left, DAUDIO_InfoPtr right) => !(left == right);

        public bool Equals(DAUDIO_InfoPtr other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is DAUDIO_InfoPtr other && this.Equals(other);

        public override int GetHashCode() => unchecked((int)this.Value);

        public override string ToString() => $"0x{(nuint)this.Value:x}";
    }
}
