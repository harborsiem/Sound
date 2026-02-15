/*
 * Copyright (c) 2007, 2021, Oracle and/or its affiliates. All rights reserved.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * This code is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 2 only, as
 * published by the Free Software Foundation.  Oracle designates this
 * particular file as subject to the "Classpath" exception as provided
 * by Oracle in the LICENSE file that accompanied this code.
 *
 * This code is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * version 2 for more details (a copy is included in the LICENSE file that
 * accompanied this code).
 *
 * You should have received a copy of the GNU General Public License version
 * 2 along with this work; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 * Please contact Oracle, 500 Oracle Parkway, Redwood Shores, CA 94065 USA
 * or visit www.oracle.com if you need additional information or have any
 * questions.
 */

//package com.sun.media.sound;

//import java.io.File;
//import java.io.FileNotFoundException;
//import java.io.IOException;
//import java.io.OutputStream;
//import java.io.RandomAccessFile;

//import static java.nio.charset.StandardCharsets.US_ASCII;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace SystemX.Media.Sound {

/**
 * Resource Interchange File Format (RIFF) stream encoder.
 *
 * @author Karl Helgason
 */
    public sealed class RIFFWriter : Stream {

        private interface IRandomAccessWriter {

            void seek(long chunksizepointer);

            long getPointer();

            void close();

            void write(int b);

            void write(byte[] b, int off, int len);

            void write(byte[] bytes);

            long length();

            void setLength(long i);
        }

        private class RandomAccessFileWriter : IRandomAccessWriter {

            internal FileStream raf;

            internal RandomAccessFileWriter(FileInfo file) {
                this.raf = new FileStream(file.FullName, FileMode.Create, FileAccess.ReadWrite);
            }

            internal RandomAccessFileWriter(String name) {
                this.raf = new FileStream(name, FileMode.Create, FileAccess.ReadWrite);
            }

            public void seek(long chunksizepointer) {
                raf.Seek(chunksizepointer, SeekOrigin.Begin);
            }

            public long getPointer() {
                return raf.Position; //getFilePointer()
            }

            public void close() {
                raf.Close();
            }

            public void write(int b) {
                raf.WriteByte((byte)b);
            }

            public void write(byte[] b, int off, int len) {
                raf.Write(b, off, len);
            }

            public void write(byte[] bytes) {
                raf.Write(bytes, 0, bytes.Length);
            }

            public long length() {
                return raf.Length;
            }

            public void setLength(long i) {
                raf.SetLength(i);
            }
        }

        private class RandomAccessByteWriter : IRandomAccessWriter {

            internal byte[] buff = new byte[32];
            internal int m_length = 0;
            internal int pos = 0;
            internal byte[] s;
            internal readonly Stream stream;

            internal RandomAccessByteWriter(Stream stream) {
                this.stream = stream;
            }

            public void seek(long chunksizepointer) {
                pos = (int)chunksizepointer;
            }

            public long getPointer() {
                return pos;
            }

            public void close() {
                stream.Write(buff, 0, m_length);
                stream.Close();
            }

            public void write(int b) {
                if (s == null)
                    s = new byte[1];
                s[0] = (byte)b;
                write(s, 0, 1);
            }

            public void write(byte[] b, int off, int len) {
                int newsize = pos + len;
                if (newsize > m_length)
                    setLength(newsize);
                int end = off + len;
                for (int i = off; i < end; i++) {
                    buff[pos++] = b[i];
                }
            }

            public void write(byte[] bytes) {
                write(bytes, 0, bytes.Length);
            }

            public long length() {
                return m_length;
            }

            public void setLength(long i) {
                m_length = (int)i;
                if (m_length > buff.Length) {
                    int newlen = Math.Max(buff.Length << 1, m_length);
                    byte[] newbuff = new byte[newlen];
                    Array.Copy(buff, 0, newbuff, 0, buff.Length);
                    buff = newbuff;
                }
            }
        }
        private int chunktype = 0; // 0=RIFF, 1=LIST; 2=CHUNK
        private IRandomAccessWriter raf;
        private readonly long chunksizepointer;
        private readonly long startpointer;
        private RIFFWriter childchunk = null;
        private bool open = true;
        private bool writeoverride = false;

        public RIFFWriter(String name, String format)
            : this(new RandomAccessFileWriter(name), format, 0) {
        }

        public RIFFWriter(FileInfo file, String format)
            : this(new RandomAccessFileWriter(file), format, 0) {
        }

        public RIFFWriter(Stream stream, String format)
            : this(new RandomAccessByteWriter(stream), format, 0) {
        }

        private RIFFWriter(IRandomAccessWriter raf, String format, int chunktype) {
            Encoding ascii = Encoding.ASCII;
            if (chunktype == 0)
                if (raf.length() != 0)
                    raf.setLength(0);
            this.raf = raf;
            if (raf.getPointer() % 2 != 0)
                raf.write(0);

            if (chunktype == 0)
                raf.write(ascii.GetBytes("RIFF"));
            else if (chunktype == 1)
                raf.write(ascii.GetBytes("LIST"));
            else
                raf.write(ascii.GetBytes((format + "    ").Substring(0, 4)));
            chunksizepointer = raf.getPointer();
            this.chunktype = 2;
            writeUnsignedInt(0);
            this.chunktype = chunktype;
            startpointer = raf.getPointer();
            if (chunktype != 2)
                raf.write(ascii.GetBytes((format + "    ").Substring(0, 4)));
        }

        public void Seek(long pos) {
            raf.seek(pos);
        }

        public long getFilePointer() {
            return raf.getPointer();
        }

        public void setWriteOverride(bool writeoverride) {
            this.writeoverride = writeoverride;
        }

        public bool getWriteOverride() {
            return writeoverride;
        }

        public override void Close() {
            if (!open)
                return;
            if (childchunk != null) {
                childchunk.Close();
                childchunk = null;
            }

            int bakchunktype = chunktype;
            long fpointer = raf.getPointer();
            raf.seek(chunksizepointer);
            chunktype = 2;
            writeUnsignedInt(fpointer - startpointer);

            if (bakchunktype == 0)
                raf.close();
            else
                raf.seek(fpointer);
            open = false;
            raf = null;
        }

        public void Write(int b) {
            if (!writeoverride) {
                if (chunktype != 2) {
                    throw new ArgumentException(
                            "Only chunks can write bytes!");
                }
                if (childchunk != null) {
                    childchunk.Close();
                    childchunk = null;
                }
            }
            raf.write(b);
        }

        public void Write(byte[] b) {
            Write(b, 0, b.Length);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (!writeoverride) {
                if (chunktype != 2) {
                    throw new ArgumentException(
                            "Only chunks can write bytes!");
                }
                if (childchunk != null) {
                    childchunk.Close();
                    childchunk = null;
                }
            }
            raf.write(buffer, offset, count);
        }

        public RIFFWriter writeList(String format) {
            if (chunktype == 2) {
                throw new ArgumentException(
                        "Only LIST and RIFF can write lists!");
            }
            if (childchunk != null) {
                childchunk.Close();
                childchunk = null;
            }
            childchunk = new RIFFWriter(this.raf, format, 1);
            return childchunk;
        }

        public RIFFWriter writeChunk(String format) {
            if (chunktype == 2) {
                throw new ArgumentException(
                        "Only LIST and RIFF can write chunks!");
            }
            if (childchunk != null) {
                childchunk.Close();
                childchunk = null;
            }
            childchunk = new RIFFWriter(this.raf, format, 2);
            return childchunk;
        }

        // Write ASCII chars to stream
        public void writeString(String _string) {
            byte[] buff = Encoding.ASCII.GetBytes(_string);
            Write(buff);
        }

        // Write ASCII chars to stream
        public void writeString(String _string, int len) {
            byte[] buff = Encoding.ASCII.GetBytes(_string);
            if (buff.Length > len)
                Write(buff, 0, len);
            else {
                Write(buff);
                for (int i = buff.Length; i < len; i++)
                    Write(0);
            }
        }

        // Write 8 bit signed integer to stream
        public void WriteSByte(int b) {
            Write(b);
        }

        // Write 16 bit signed integer to stream
        public void writeShort(short b) {
            Write(((ushort)b >> 0) & 0xFF);
            Write(((ushort)b >> 8) & 0xFF);
        }

        // Write 32 bit signed integer to stream
        public void writeInt(int b) {
            Write((int)((uint)b >> 0) & 0xFF);
            Write((int)((uint)b >> 8) & 0xFF);
            Write((int)((uint)b >> 16) & 0xFF);
            Write((int)((uint)b >> 24) & 0xFF);
        }

        // Write 64 bit signed integer to stream
        public void writeLong(long b) {
            Write((int)((ulong)b >> 0) & 0xFF);
            Write((int)((ulong)b >> 8) & 0xFF);
            Write((int)((ulong)b >> 16) & 0xFF);
            Write((int)((ulong)b >> 24) & 0xFF);
            Write((int)((ulong)b >> 32) & 0xFF);
            Write((int)((ulong)b >> 40) & 0xFF);
            Write((int)((ulong)b >> 48) & 0xFF);
            Write((int)((ulong)b >> 56) & 0xFF);
        }

        // Write 8 bit unsigned integer to stream
        public void writeUnsignedByte(int b) {
            WriteSByte((byte)b);
        }

        // Write 16 bit unsigned integer to stream
        public void writeUnsignedShort(int b) {
            writeShort((short)b);
        }

        // Write 32 bit unsigned integer to stream
        public void writeUnsignedInt(long b) {
            writeInt((int)b);
        }

        public override int ReadByte() {
            throw new NotSupportedException("RiffWriter ReadByte not supported");
        }

        public override int Read(byte[] buffer, int offset, int count) {
            throw new NotSupportedException("RiffWriter Read not supported");
        }

        public override void WriteByte(byte value) {
            throw new NotSupportedException("RiffWriter WriteByte not supported");
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            throw new NotSupportedException("RiffWriter BeginRead not supported");
        }

        public override int EndRead(IAsyncResult asyncResult) {
            throw new NotSupportedException("RiffWriter EndRead not supported");
        }

        public override void SetLength(long value) {
            raf.setLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            if (origin == SeekOrigin.Begin) {
                Seek(offset);
                return offset;
            }
            throw new NotSupportedException("RiffWriter Seek not supported");
        }

        public override void Flush() {
            //throw new NotSupportedException("RiffWriter Flush not supported");
        }

        public override long Position {
            get { return getFilePointer(); }
            set { throw new NotSupportedException("RiffWriter Position not supported"); }
        }

        public override long Length {
            get { return raf.length(); }
        }

        public override bool CanRead {
            get { return false; }
        }

        public override bool CanWrite {
            get { return true; }
        }

        public override bool CanSeek {
            get { return true; }
        }
    }
}
