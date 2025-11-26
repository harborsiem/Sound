using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SystemX.Media.Sound {
    [DebuggerDisplay("{Value}")]
    internal unsafe readonly partial struct PortControlCreatorPtr : IEquatable<PortControlCreatorPtr> {
        internal readonly void* Value;

        internal PortControlCreatorPtr(void* value) => this.Value = value;

        internal PortControlCreatorPtr(UIntPtr value) : this((void*)value) {
        }

        internal static PortControlCreatorPtr Null => default;

        internal bool IsNull => Value == default;

        public static implicit operator void*(PortControlCreatorPtr value) => value.Value;

        public static explicit operator PortControlCreatorPtr(void* value) => new PortControlCreatorPtr(value);

        public static bool operator ==(PortControlCreatorPtr left, PortControlCreatorPtr right) => left.Value == right.Value;

        public static bool operator !=(PortControlCreatorPtr left, PortControlCreatorPtr right) => !(left == right);

        public bool Equals(PortControlCreatorPtr other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is PortControlCreatorPtr other && this.Equals(other);

        public override int GetHashCode() => unchecked((int)this.Value);

        public override string ToString() => $"0x{(nuint)this.Value:x}";
    }
}
