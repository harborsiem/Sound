using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SystemX.Media.Sound {
    [DebuggerDisplay("{Value}")]
    internal unsafe readonly partial struct MidiDeviceHandlePtr : IEquatable<MidiDeviceHandlePtr> {
        internal readonly void* Value;

        internal MidiDeviceHandlePtr(void* value) => this.Value = value;

        internal MidiDeviceHandlePtr(IntPtr value) : this((void*)value) {
        }

        internal static MidiDeviceHandlePtr Null => default;

        internal bool IsNull => Value == default;

        public static implicit operator void*(MidiDeviceHandlePtr value) => value.Value;

        public static explicit operator MidiDeviceHandlePtr(void* value) => new MidiDeviceHandlePtr(value);

        public static bool operator ==(MidiDeviceHandlePtr left, MidiDeviceHandlePtr right) => left.Value == right.Value;

        public static bool operator !=(MidiDeviceHandlePtr left, MidiDeviceHandlePtr right) => !(left == right);

        public bool Equals(MidiDeviceHandlePtr other) => this.Value == other.Value;

        public override bool Equals(object obj) => obj is MidiDeviceHandlePtr other && this.Equals(other);

        public override int GetHashCode() => unchecked((int)this.Value);

        public override string ToString() => $"0x{(nuint)this.Value:x}";
    }
}
