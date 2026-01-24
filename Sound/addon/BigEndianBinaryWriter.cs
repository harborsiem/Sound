using System;
using System.Collections.Generic;
using System.Text;

namespace System.IO {
    public sealed class BigEndianBinaryWriter : BinaryWriter {

        private bool isBigEndian;

        public bool IsBigEndian {
            get { return isBigEndian; }
            private set { isBigEndian = value; }
        }

        public BigEndianBinaryWriter(Stream output)
            : base(output) {
            IsBigEndian = true;
        }

        public BigEndianBinaryWriter(Stream output, Encoding encoding)
            : base(output, encoding) {
            IsBigEndian = true;
        }

        //public BigEndianBinaryWriter(Stream output, Encoding encoding, bool leaveOpen)
        //    : base(output, encoding, leaveOpen) {
        //    IsBigEndian = true;
        //}

        public override void Write(decimal value) {
            if (!IsBigEndian) {
                base.Write(value);
                return;
            }
            int[] intArray = decimal.GetBits(value);
            byte[] buffer = new byte[sizeof(decimal)];
            Buffer.BlockCopy(intArray, 0, buffer, 0, sizeof(decimal));
            Array.Reverse(buffer);
            base.Write(buffer, 0, 16);
        }

        public override void Write(double value) {
            if (!IsBigEndian) {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 8);
        }

        public override void Write(float value) {
            if (!IsBigEndian) {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 4);
        }

        public override void Write(int value) {
            if (!IsBigEndian) {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 4);
        }

        public override void Write(long value) {
            if (!IsBigEndian) {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 8);
        }

        public override void Write(short value) {
            if (!IsBigEndian) {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 2);
        }

        [CLSCompliant(false)]
        public override void Write(uint value) {
            if (!IsBigEndian) {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 4);
        }

        [CLSCompliant(false)]
        public override void Write(ulong value) {
            if (!IsBigEndian) {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 8);
        }

        [CLSCompliant(false)]
        public override void Write(ushort value) {
            if (!IsBigEndian) {
                base.Write(value);
                return;
            }
            byte[] buffer = BitConverter.GetBytes(value);
            Array.Reverse(buffer);
            base.Write(buffer, 0, 2);
        }
    }
}
