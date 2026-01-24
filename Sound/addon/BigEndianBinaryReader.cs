using System;
using System.Collections.Generic;
using System.Text;

namespace System.IO {
    public sealed class BigEndianBinaryReader : BinaryReader {

        private bool isBigEndian;

        public bool IsBigEndian {
            get { return isBigEndian; }
            private set { isBigEndian = value; }
        }

        public BigEndianBinaryReader(Stream input)
            : base(input) {
            IsBigEndian = true;
        }

        public BigEndianBinaryReader(Stream input, Encoding encoding)
            : base(input, encoding) {
            IsBigEndian = true;
        }

        //public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen)
        //    : base(input, encoding, leaveOpen) {
        //    IsBigEndian = true;
        //}

        public override decimal ReadDecimal() {
            if (!IsBigEndian) {
                return base.ReadDecimal();
            }
            byte[] buffer = base.ReadBytes(sizeof(decimal));
            if (buffer.Length != sizeof(decimal)) {
                throw new EndOfStreamException();
            }
            int[] intArray = new int[4];
            intArray[0] = ((buffer[15] | (buffer[14] << 8)) | (buffer[13] << 0x10)) | (buffer[12] << 0x18);
            intArray[1] = ((buffer[11] | (buffer[10] << 8)) | (buffer[9] << 0x10)) | (buffer[8] << 0x18);
            intArray[2] = ((buffer[7] | (buffer[6] << 8)) | (buffer[5] << 0x10)) | (buffer[4] << 0x18);
            intArray[3] = ((buffer[3] | (buffer[2] << 8)) | (buffer[1] << 0x10)) | (buffer[0] << 0x18);
            return new decimal(intArray);
        }

        public override double ReadDouble() {
            if (!IsBigEndian) {
                return base.ReadDouble();
            }
            byte[] buffer = base.ReadBytes(sizeof(double));
            if (buffer.Length != sizeof(double)) {
                throw new EndOfStreamException();
            }
            Array.Reverse(buffer);
            return BitConverter.ToDouble(buffer, 0);
        }

        public override short ReadInt16() {
            if (!IsBigEndian) {
                return base.ReadInt16();
            }
            byte[] buffer = base.ReadBytes(sizeof(short));
            if (buffer.Length != sizeof(short)) {
                throw new EndOfStreamException();
            }
            return (short)(buffer[1] | (buffer[0] << 8));
        }

        public override int ReadInt32() {
            if (!IsBigEndian) {
                return base.ReadInt32();
            }
            byte[] buffer = base.ReadBytes(sizeof(int));
            if (buffer.Length != sizeof(int)) {
                throw new EndOfStreamException();
            }
            return (((buffer[3] | (buffer[2] << 8)) | (buffer[1] << 0x10)) | (buffer[0] << 0x18));
        }

        public override long ReadInt64() {
            if (!IsBigEndian) {
                return base.ReadInt64();
            }
            byte[] buffer = base.ReadBytes(sizeof(long));
            if (buffer.Length != sizeof(long)) {
                throw new EndOfStreamException();
            }
            uint num = (uint)(((buffer[7] | (buffer[6] << 8)) | (buffer[5] << 0x10)) | (buffer[4] << 0x18));
            uint num2 = (uint)(((buffer[3] | (buffer[2] << 8)) | (buffer[1] << 0x10)) | (buffer[0] << 0x18));
            return (long)(((ulong)num2 << 0x20) | num);
        }

        public override float ReadSingle() {
            if (!IsBigEndian) {
                return base.ReadSingle();
            }
            byte[] buffer = base.ReadBytes(sizeof(float));
            if (buffer.Length != sizeof(float)) {
                throw new EndOfStreamException();
            }
            Array.Reverse(buffer);
            return BitConverter.ToSingle(buffer, 0);
        }

        [CLSCompliant(false)]
        public override ushort ReadUInt16() {
            if (!IsBigEndian) {
                return base.ReadUInt16();
            }
            byte[] buffer = base.ReadBytes(sizeof(ushort));
            if (buffer.Length != sizeof(ushort)) {
                throw new EndOfStreamException();
            }
            return (ushort)(buffer[1] | (buffer[0] << 8));
        }

        [CLSCompliant(false)]
        public override uint ReadUInt32() {
            if (!IsBigEndian) {
                return base.ReadUInt32();
            }
            byte[] buffer = base.ReadBytes(sizeof(uint));
            if (buffer.Length != sizeof(uint)) {
                throw new EndOfStreamException();
            }
            return (uint)(((buffer[3] | (buffer[2] << 8)) | (buffer[1] << 0x10)) | (buffer[0] << 0x18));
        }

        [CLSCompliant(false)]
        public override ulong ReadUInt64() {
            if (!IsBigEndian) {
                return base.ReadUInt64();
            }
            byte[] buffer = base.ReadBytes(sizeof(ulong));
            if (buffer.Length != sizeof(ulong)) {
                throw new EndOfStreamException();
            }
            uint num = (uint)(((buffer[7] | (buffer[6] << 8)) | (buffer[5] << 0x10)) | (buffer[4] << 0x18));
            uint num2 = (uint)(((buffer[3] | (buffer[2] << 8)) | (buffer[1] << 0x10)) | (buffer[0] << 0x18));
            return (ulong)(((ulong)num2 << 0x20) | num);
        }
    }
}
