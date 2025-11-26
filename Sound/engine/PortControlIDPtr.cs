using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SystemX.Media.Sound {
    [DebuggerDisplay("{Value}")]
    internal unsafe readonly partial struct PortControlIDPtr : IEquatable<PortControlIDPtr> {
        internal readonly void* Value;

        internal PortControlIDPtr(void* value) => this.Value = value;

        internal PortControlIDPtr(UIntPtr value) : this((void*)value) {
        }

        internal static PortControlIDPtr Null => default;

        internal bool IsNull => Value == default;

        public static implicit operator void*(PortControlIDPtr value) => value.Value;

        public static explicit operator PortControlIDPtr(void* value) => new PortControlIDPtr(value);

        public static bool operator ==(PortControlIDPtr left, PortControlIDPtr right) => left.Value == right.Value;

        public static bool operator !=(PortControlIDPtr left, PortControlIDPtr right) => !(left == right);

        public bool Equals(PortControlIDPtr other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is PortControlIDPtr other && this.Equals(other);

        public override int GetHashCode() => unchecked((int)this.Value);

        public override string ToString() => $"0x{(nuint)this.Value:x}";
    }
}
