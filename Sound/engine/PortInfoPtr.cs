using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SystemX.Media.Sound {
    [DebuggerDisplay("{Value}")]
    internal unsafe readonly partial struct PortInfoPtr : IEquatable<PortInfoPtr> {
        internal readonly void* Value;

        internal PortInfoPtr(void* value) => this.Value = value;

        internal PortInfoPtr(IntPtr value) : this((void*)value) {
        }

        internal static PortInfoPtr Null => default;

        internal bool IsNull => Value == default;

        public static implicit operator void*(PortInfoPtr value) => value.Value;

        public static explicit operator PortInfoPtr(void* value) => new PortInfoPtr(value);

        public static bool operator ==(PortInfoPtr left, PortInfoPtr right) => left.Value == right.Value;

        public static bool operator !=(PortInfoPtr left, PortInfoPtr right) => !(left == right);

        public bool Equals(PortInfoPtr other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is PortInfoPtr other && this.Equals(other);

        public override int GetHashCode() => unchecked((int)this.Value);

        public override string ToString() => $"0x{(nuint)this.Value:x}";
    }
}
