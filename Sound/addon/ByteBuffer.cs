using System;
using System.Collections.Generic;
using System.Text;

namespace System.IO {
    public enum ByteOrder {
        BigEndian,
        LittleEndian
    }

    //public abstract class Buffer {
    //    // Invariants: mark <= position <= limit <= capacity
    //    private int _mark = -1;
    //    private int _position = 0;
    //    private int _limit;
    //    private int _capacity;

    //    internal protected Buffer(int mark, int pos, int lim, int cap) {
    //        if (cap < 0)
    //            throw new ArgumentException();
    //        this._capacity = cap;
    //        limit(lim);
    //        position(pos);
    //        if (mark >= 0) {
    //            if (mark > pos)
    //                throw new ArgumentException();
    //            this._mark = mark;
    //        }
    //    }

    //    public int capacity() {
    //        return _capacity;
    //    }

    //    public Buffer position(int newPosition) {
    //        if ((newPosition > _limit) || (newPosition < 0))
    //            throw new ArgumentException();
    //        _position = newPosition;
    //        if (_mark > _position) _mark = -1;
    //        return this;
    //    }

    //    public Buffer limit(int newLimit) {
    //        if ((newLimit > _capacity) || (newLimit < 0))
    //            throw new ArgumentException();
    //        _limit = newLimit;
    //        if (_position > _limit) _position = _limit;
    //        if (_mark > _limit) _mark = -1;
    //        return this;
    //    }

    //}

    public sealed class ByteBuffer : IDisposable {
        private const String argMessage = "Length of ByteBuffer is not modulo for this buffer";
        private ByteBuffer m_instance;
        private ByteOrder m_order;
        private int m_length;
        private MemoryStream stream;
        private BinaryReader reader;
        private BinaryWriter writer;
        private bool disposed;
        private int lastOrder = -1;
        private int lastPosition;

        private ByteBuffer(int length) {
            m_instance = this;
            m_length = length;
            m_order = ByteOrder.BigEndian;
            stream = new MemoryStream(new byte[length], 0, length, true, true);
            SetReaderWriter(m_order);
        }

        private ByteBuffer(byte[] buffer) {
            m_instance = this;
            m_length = buffer.Length;
            m_order = ByteOrder.BigEndian;
            stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
            SetReaderWriter(m_order);
        }

        private void SetReaderWriter(ByteOrder order) {
            if (lastOrder != (int)order) {
                if (order == ByteOrder.BigEndian) {
                    reader = new BigEndianBinaryReader(stream);
                    writer = new BigEndianBinaryWriter(stream);
                } else {
                    reader = new BinaryReader(stream);
                    writer = new BinaryWriter(stream);
                }
                lastOrder = (int)order;
            }
        }

        public void Dispose() {
            if (!disposed) {
                if (reader != null) {
                    reader.Close();
                }
                if (writer != null) {
                    writer.Close();
                }
                if (stream != null) {
                    stream.Close();
                }
                GC.SuppressFinalize(this);
            }
            disposed = true;
        }

        public static ByteBuffer Allocate(int length) {
            ByteBuffer bbuffer = new ByteBuffer(length);
            return bbuffer;
        }

        public static ByteBuffer Allocate(byte[] buffer) {
            ByteBuffer bbuffer = new ByteBuffer(buffer);
            return bbuffer;
        }

        public ByteOrder Order() {
            return m_order;
        }

        public ByteBuffer Order(ByteOrder order) {
            m_order = order;
            SetReaderWriter(order);
            return m_instance;
        }

        public int Capacity() {
            return m_length;
        }

        public byte[] GetBuffer() {
            return stream.GetBuffer();
        }

        public void Read(byte[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            stream.Read(buffer, offset, count);
            lastPosition = (int)stream.Position;
        }

        public void Write(byte[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            stream.Write(buffer, offset, count);
            lastPosition = (int)stream.Position;
        }

        public void Write(byte value) {
            stream.Position = lastPosition;
            stream.WriteByte(value);
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public SByteBuffer AsSByteBuffer() {
            return new SByteBuffer(m_instance, reader, writer);
        }

        public Int16Buffer AsInt16Buffer() {
            if (m_length % sizeof(Int16) != 0) {
                throw new ArgumentException(argMessage);
            }
            return new Int16Buffer(m_instance, reader, writer);
        }

        [CLSCompliant(false)]
        public UInt16Buffer AsUInt16Buffer() {
            if (m_length % sizeof(UInt16) != 0) {
                throw new ArgumentException(argMessage);
            }
            return new UInt16Buffer(m_instance, reader, writer);
        }

        public Int32Buffer AsInt32Buffer() {
            if (m_length % sizeof(Int32) != 0) {
                throw new ArgumentException(argMessage);
            }
            return new Int32Buffer(m_instance, reader, writer);
        }

        [CLSCompliant(false)]
        public UInt32Buffer AsUInt32Buffer() {
            if (m_length % sizeof(UInt32) != 0) {
                throw new ArgumentException(argMessage);
            }
            return new UInt32Buffer(m_instance, reader, writer);
        }

        public Int64Buffer AsInt64Buffer() {
            if (m_length % sizeof(Int64) != 0) {
                throw new ArgumentException(argMessage);
            }
            return new Int64Buffer(m_instance, reader, writer);
        }

        [CLSCompliant(false)]
        public UInt64Buffer AsUInt64Buffer() {
            if (m_length % sizeof(UInt64) != 0) {
                throw new ArgumentException(argMessage);
            }
            return new UInt64Buffer(m_instance, reader, writer);
        }

        public SingleBuffer AsSingleBuffer() {
            if (m_length % sizeof(Single) != 0) {
                throw new ArgumentException(argMessage);
            }
            return new SingleBuffer(m_instance, reader, writer); ;
        }

        public DoubleBuffer AsDoubleBuffer() {
            if (m_length % sizeof(Double) != 0) {
                throw new ArgumentException(argMessage);
            }
            return new DoubleBuffer(m_instance, reader, writer);
        }

        public void Position(int offset) {
            if (offset < 0 || offset >= (int)stream.Length) {
                throw new ArgumentOutOfRangeException("offset", "to big");
            }
            lastPosition = offset;
        }
    }

    public sealed class SByteBuffer {
        private ByteBuffer bBuffer;
        private BinaryReader reader;
        private BinaryWriter writer;
        private Stream stream;
        private int lastPosition;

        internal SByteBuffer(ByteBuffer bBuffer, BinaryReader reader, BinaryWriter writer) {
            this.bBuffer = bBuffer;
            this.reader = reader;
            this.writer = writer;
            this.stream = reader.BaseStream;
        }

        public void Position(int offset) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "< 0");
            }
            lastPosition = offset;
        }

        [CLSCompliant(false)]
        public void Read(sbyte[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                buffer[i] = reader.ReadSByte();
            }
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public void Write(sbyte[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                writer.Write(buffer[i]);
            }
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public void Write(sbyte value) {
            stream.Position = lastPosition;
            writer.Write(value);
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public sbyte[] ToArray() {
            byte[] buffer = bBuffer.GetBuffer();
            sbyte[] result = new sbyte[buffer.Length];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            return result;
        }
    }

    public sealed class Int16Buffer {
        private ByteBuffer bBuffer;
        private BinaryReader reader;
        private BinaryWriter writer;
        private Stream stream;
        private int lastPosition;

        internal Int16Buffer(ByteBuffer bBuffer, BinaryReader reader, BinaryWriter writer) {
            this.bBuffer = bBuffer;
            this.reader = reader;
            this.writer = writer;
            this.stream = reader.BaseStream;
        }

        public void Position(int offset) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "< 0");
            }
            lastPosition = offset * sizeof(short);
        }

        public void Read(short[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                buffer[i] = reader.ReadInt16();
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(short[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                writer.Write(buffer[i]);
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(short value) {
            stream.Position = lastPosition;
            writer.Write(value);
            lastPosition = (int)stream.Position;
        }

        public short[] ToArray() {
            byte[] buffer = bBuffer.GetBuffer();
            short[] result = new short[buffer.Length / sizeof(short)];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            if (bBuffer.Order() == ByteOrder.BigEndian) {
                Array.Reverse(result, 0, result.Length);
            }
            return result;
        }
    }

    public sealed class UInt16Buffer {
        private ByteBuffer bBuffer;
        private BinaryReader reader;
        private BinaryWriter writer;
        private Stream stream;
        private int lastPosition;

        internal UInt16Buffer(ByteBuffer bBuffer, BinaryReader reader, BinaryWriter writer) {
            this.bBuffer = bBuffer;
            this.reader = reader;
            this.writer = writer;
            this.stream = reader.BaseStream;
        }

        public void Position(int offset) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "< 0");
            }
            lastPosition = offset * sizeof(ushort);
        }

        [CLSCompliant(false)]
        public void Read(ushort[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                buffer[i] = reader.ReadUInt16();
            }
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public void Write(ushort[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                writer.Write(buffer[i]);
            }
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public void Write(ushort value) {
            stream.Position = lastPosition;
            writer.Write(value);
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public ushort[] ToArray() {
            byte[] buffer = bBuffer.GetBuffer();
            ushort[] result = new ushort[buffer.Length / sizeof(ushort)];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            if (bBuffer.Order() == ByteOrder.BigEndian) {
                Array.Reverse(result, 0, result.Length);
            }
            return result;
        }
    }

    public class Int32Buffer {
        private ByteBuffer bBuffer;
        private BinaryReader reader;
        private BinaryWriter writer;
        private Stream stream;
        private int lastPosition;

        internal Int32Buffer(ByteBuffer bBuffer, BinaryReader reader, BinaryWriter writer) {
            this.bBuffer = bBuffer;
            this.reader = reader;
            this.writer = writer;
            this.stream = reader.BaseStream;
        }

        public void Position(int offset) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "< 0");
            }
            lastPosition = offset * sizeof(int);
        }

        public void Read(int[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                buffer[i] = reader.ReadInt32();
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(int[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                writer.Write(buffer[i]);
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(int value) {
            stream.Position = lastPosition;
            writer.Write(value);
            lastPosition = (int)stream.Position;
        }

        public int[] ToArray() {
            byte[] buffer = bBuffer.GetBuffer();
            int[] result = new int[buffer.Length / sizeof(int)];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            if (bBuffer.Order() == ByteOrder.BigEndian) {
                Array.Reverse(result, 0, result.Length);
            }
            return result;
        }
    }

    public class UInt32Buffer {
        private ByteBuffer bBuffer;
        private BinaryReader reader;
        private BinaryWriter writer;
        private Stream stream;
        private int lastPosition;

        internal UInt32Buffer(ByteBuffer bBuffer, BinaryReader reader, BinaryWriter writer) {
            this.bBuffer = bBuffer;
            this.reader = reader;
            this.writer = writer;
            this.stream = reader.BaseStream;
        }

        public void Position(int offset) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "< 0");
            }
            lastPosition = offset * sizeof(uint);
        }

        [CLSCompliant(false)]
        public void Read(uint[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                buffer[i] = reader.ReadUInt32();
            }
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public void Write(uint[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                writer.Write(buffer[i]);
            }
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public void Write(uint value) {
            stream.Position = lastPosition;
            writer.Write(value);
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public uint[] ToArray() {
            byte[] buffer = bBuffer.GetBuffer();
            uint[] result = new uint[buffer.Length / sizeof(uint)];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            if (bBuffer.Order() == ByteOrder.BigEndian) {
                Array.Reverse(result, 0, result.Length);
            }
            return result;
        }
    }

    public class Int64Buffer {
        private ByteBuffer bBuffer;
        private BinaryReader reader;
        private BinaryWriter writer;
        private Stream stream;
        private int lastPosition;

        internal Int64Buffer(ByteBuffer bBuffer, BinaryReader reader, BinaryWriter writer) {
            this.bBuffer = bBuffer;
            this.reader = reader;
            this.writer = writer;
            this.stream = reader.BaseStream;
        }

        public void Position(int offset) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "< 0");
            }
            lastPosition = offset * sizeof(long);
        }

        public void Read(long[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                buffer[i] = reader.ReadInt64();
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(long[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                writer.Write(buffer[i]);
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(long value) {
            stream.Position = lastPosition;
            writer.Write(value);
            lastPosition = (int)stream.Position;
        }

        public long[] ToArray() {
            byte[] buffer = bBuffer.GetBuffer();
            long[] result = new long[buffer.Length / sizeof(long)];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            if (bBuffer.Order() == ByteOrder.BigEndian) {
                Array.Reverse(result, 0, result.Length);
            }
            return result;
        }
    }

    public class UInt64Buffer {
        private ByteBuffer bBuffer;
        private BinaryReader reader;
        private BinaryWriter writer;
        private Stream stream;
        private int lastPosition;

        internal UInt64Buffer(ByteBuffer bBuffer, BinaryReader reader, BinaryWriter writer) {
            this.bBuffer = bBuffer;
            this.reader = reader;
            this.writer = writer;
            this.stream = reader.BaseStream;
        }

        public void Position(int offset) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "< 0");
            }
            lastPosition = offset * sizeof(ulong);
        }

        [CLSCompliant(false)]
        public void Read(ulong[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                buffer[i] = reader.ReadUInt64();
            }
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public void Write(ulong[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                writer.Write(buffer[i]);
            }
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public void Write(ulong value) {
            stream.Position = lastPosition;
            writer.Write(value);
            lastPosition = (int)stream.Position;
        }

        [CLSCompliant(false)]
        public ulong[] ToArray() {
            byte[] buffer = bBuffer.GetBuffer();
            ulong[] result = new ulong[buffer.Length / sizeof(ulong)];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            if (bBuffer.Order() == ByteOrder.BigEndian) {
                Array.Reverse(result, 0, result.Length);
            }
            return result;
        }
    }

    public class SingleBuffer {
        private ByteBuffer bBuffer;
        private BinaryReader reader;
        private BinaryWriter writer;
        private Stream stream;
        private int lastPosition;

        internal SingleBuffer(ByteBuffer bBuffer, BinaryReader reader, BinaryWriter writer) {
            this.bBuffer = bBuffer;
            this.reader = reader;
            this.writer = writer;
            this.stream = reader.BaseStream;
        }

        public void Position(int offset) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "< 0");
            }
            lastPosition = offset * sizeof(float);
        }

        public void Read(float[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                buffer[i] = reader.ReadSingle();
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(float[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                writer.Write(buffer[i]);
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(float value) {
            stream.Position = lastPosition;
            writer.Write(value);
            lastPosition = (int)stream.Position;
        }

        public float[] ToArray() {
            byte[] buffer = bBuffer.GetBuffer();
            float[] result = new float[buffer.Length / sizeof(float)];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            if (bBuffer.Order() == ByteOrder.BigEndian) {
                Array.Reverse(result, 0, result.Length);
            }
            return result;
        }
    }

    public class DoubleBuffer {
        private ByteBuffer bBuffer;
        private BinaryReader reader;
        private BinaryWriter writer;
        private Stream stream;
        private int lastPosition;

        internal DoubleBuffer(ByteBuffer bBuffer, BinaryReader reader, BinaryWriter writer) {
            this.bBuffer = bBuffer;
            this.reader = reader;
            this.writer = writer;
            this.stream = reader.BaseStream;
        }

        public void Position(int offset) {
            if (offset < 0) {
                throw new ArgumentOutOfRangeException("offset", "< 0");
            }
            lastPosition = offset * sizeof(double);
        }

        public void Read(double[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                buffer[i] = reader.ReadDouble();
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(double[] buffer, int offset, int count) {
            stream.Position = lastPosition;
            for (int i = offset; i < offset + count; i++) {
                writer.Write(buffer[i]);
            }
            lastPosition = (int)stream.Position;
        }

        public void Write(double value) {
            stream.Position = lastPosition;
            writer.Write(value);
            lastPosition = (int)stream.Position;
        }

        public double[] ToArray() {
            byte[] buffer = bBuffer.GetBuffer();
            double[] result = new double[buffer.Length / sizeof(double)];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);
            if (bBuffer.Order() == ByteOrder.BigEndian) {
                Array.Reverse(result, 0, result.Length);
            }
            return result;
        }
    }
}
