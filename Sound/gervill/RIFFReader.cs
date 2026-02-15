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

//import java.io.EOFException;
//import java.io.IOException;
//import java.io.InputStream;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

namespace SystemX.Media.Sound {

/**
 * Resource Interchange File Format (RIFF) stream decoder.
 *
 * @author Karl Helgason
 */
    public sealed class RIFFReader : InputStream {

        private readonly RIFFReader root;
        private long filepointer = 0;
        private readonly String fourcc;
        private String riff_type = null;
        private readonly long ckSize;
        private readonly InputStream stream;
        private long avail = 0xffffffffL; // MAX_UNSIGNED_INT
        private RIFFReader lastiterator = null;

        public RIFFReader(Stream stream) {

            if (stream is RIFFReader) {
                root = ((RIFFReader)stream).root;
            } else {
                root = this;
            }

            this.stream = stream as InputStream;
            if (this.stream == null && stream != null) {
                this.stream = new InputStreamImpl(stream);
            }

            // Check for RIFF null paddings,
            int b;
            while (true) {
                b = ReadByte();
                if (b == -1) {
                    this.fourcc = ""; // don't put null value into fourcc,
                    // because it is expected to
                    // always contain a string value
                    riff_type = null;
                    ckSize = 0;
                    avail = 0;
                    return;
                }
                if (b != 0) {
                    break;
                }
            }

            byte[] fourcc = new byte[4];
            fourcc[0] = (byte)b;
            readFully(fourcc, 1, 3);
            this.fourcc = Encoding.ASCII.GetString(fourcc);
            ckSize = readUnsignedInt();

            avail = ckSize;

            if (getFormat().Equals("RIFF") || getFormat().Equals("LIST")) {
                byte[] format = new byte[4];
                readFully(format);
                this.riff_type = Encoding.ASCII.GetString(format);
            }
        }

        public override void SetLength(long value) {
            stream.SetLength(value);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return stream.Seek(offset, origin);
        }

        public override long Position {
            get { return stream.Position; }
            set { stream.Position = value; }
        }

        public override long Length {
            get { return stream.Length; }
        }

        public override bool CanSeek {
            get { return true; }
        }

        public long getFilePointer() {
            return root.filepointer;
        }

        public bool hasNextChunk() {
            if (lastiterator != null)
                lastiterator.finish();
            return avail != 0;
        }

        public RIFFReader nextChunk() {
            if (lastiterator != null)
                lastiterator.finish();
            if (avail == 0)
                return null;
            lastiterator = new RIFFReader(this);
            return lastiterator;
        }

        public String getFormat() {
            return fourcc;
        }

        public String getRiffType() {
            return riff_type;
        }

        public long getSize() {
            return ckSize;
        }

        public override int ReadByte() {
            if (avail == 0) {
                return -1;
            }
            int b = stream.ReadByte();
            if (b == -1) {
                avail = 0;
                return -1;
            }
            avail--;
            filepointer++;
            return b;
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (avail == 0) {
                return -1;
            }
            if (count > avail) {
                int rlen = stream.Read(buffer, offset, (int)avail);
                if (rlen > 0)
                    filepointer += rlen;
                avail = 0;
                return rlen;
            } else {
                int ret = stream.Read(buffer, offset, count);
                if (ret <= 0) {
                    avail = 0;
                    return 0;
                }
                avail -= ret;
                filepointer += ret;
                return ret;
            }
        }

        public void readFully(byte[] b) { //sealed
            readFully(b, 0, b.Length);
        }

        public void readFully(byte[] b, int off, int len) { //sealed
            if (len < 0)
                throw new ArgumentException("< 0", "len");
            while (len > 0) {
                int s = Read(b, off, len);
                if (s < 0)
                    throw new EndOfStreamException();
                if (s == 0)
                    //Thread.Sleep(0);
                    Thread.Yield();
                off += s;
                len -= s;
            }
        }

        public override long skip(long n) {
            if (n <= 0 || avail == 0) {
                return 0;
            }
            // will not skip more than
            long remaining = Math.Min(n, avail);
            while (remaining > 0) {
                // Some input streams like FileInputStream can return more bytes,
                // when EOF is reached.
                long ret = Math.Min(stream.skip(remaining), remaining);
                if (ret == 0) {
                    // EOF or not? we need to check.
                    Thread.Yield();
                    if (stream.ReadByte() == -1) {
                        avail = 0;
                        break;
                    }
                    ret = 1;
                }
                else if (ret < 0)
                {
                    // the skip should not return negative value, but check it also
                    avail = 0;
                    break;
                }
                remaining -= ret;
                avail -= ret;
                filepointer += ret;
            }
            return n - remaining;
        }

        public override int available() {
            return avail > Int32.MaxValue ? Int32.MaxValue : (int)avail;
        }

        public void finish() {
            if (avail != 0) {
                skip(avail);
            }
        }

        // Read ASCII chars from stream
        public String readString(int len) {
            byte[] buff;
            try {
                buff = new byte[len];
            }
            catch (OutOfMemoryException oom) {
                throw new IOException("Length too big", oom);
            }
            readFully(buff);
            for (int i = 0; i < buff.Length; i++) {
                if (buff[i] == 0) {
                    return Encoding.ASCII.GetString(buff, 0, i);
                }
            }
            return Encoding.ASCII.GetString(buff);
        }

        // Read 8 bit signed integer from stream
        public byte readSByte() {
            int ch = ReadByte();
            if (ch < 0)
                throw new EndOfStreamException();
            return (byte)ch;
        }

        // Read 16 bit signed integer from stream
        public short readShort() {
            int ch1 = ReadByte();
            int ch2 = ReadByte();
            if (ch1 < 0)
                throw new EndOfStreamException();
            if (ch2 < 0)
                throw new EndOfStreamException();
            return (short)(ch1 | (ch2 << 8));
        }

        // Read 32 bit signed integer from stream
        public int readInt() {
            int ch1 = ReadByte();
            int ch2 = ReadByte();
            int ch3 = ReadByte();
            int ch4 = ReadByte();
            if (ch1 < 0)
                throw new EndOfStreamException();
            if (ch2 < 0)
                throw new EndOfStreamException();
            if (ch3 < 0)
                throw new EndOfStreamException();
            if (ch4 < 0)
                throw new EndOfStreamException();
            return ch1 + (ch2 << 8) | (ch3 << 16) | (ch4 << 24);
        }

        // Read 64 bit signed integer from stream
        public long readLong() {
            long ch1 = ReadByte();
            long ch2 = ReadByte();
            long ch3 = ReadByte();
            long ch4 = ReadByte();
            long ch5 = ReadByte();
            long ch6 = ReadByte();
            long ch7 = ReadByte();
            long ch8 = ReadByte();
            if (ch1 < 0)
                throw new EndOfStreamException();
            if (ch2 < 0)
                throw new EndOfStreamException();
            if (ch3 < 0)
                throw new EndOfStreamException();
            if (ch4 < 0)
                throw new EndOfStreamException();
            if (ch5 < 0)
                throw new EndOfStreamException();
            if (ch6 < 0)
                throw new EndOfStreamException();
            if (ch7 < 0)
                throw new EndOfStreamException();
            if (ch8 < 0)
                throw new EndOfStreamException();
            return ch1 | (ch2 << 8) | (ch3 << 16) | (ch4 << 24)
                    | (ch5 << 32) | (ch6 << 40) | (ch7 << 48) | (ch8 << 56);
        }

        // Read 8 bit unsigned integer from stream
        public int readUnsignedByte() {
            int ch = ReadByte();
            if (ch < 0)
                throw new EndOfStreamException();
            return ch;
        }

        // Read 16 bit unsigned integer from stream
        public int readUnsignedShort() {
            int ch1 = ReadByte();
            int ch2 = ReadByte();
            if (ch1 < 0)
                throw new EndOfStreamException();
            if (ch2 < 0)
                throw new EndOfStreamException();
            return ch1 | (ch2 << 8);
        }

        // Read 32 bit unsigned integer from stream
        public long readUnsignedInt() {
            long ch1 = ReadByte();
            long ch2 = ReadByte();
            long ch3 = ReadByte();
            long ch4 = ReadByte();
            if (ch1 < 0)
                throw new EndOfStreamException();
            if (ch2 < 0)
                throw new EndOfStreamException();
            if (ch3 < 0)
                throw new EndOfStreamException();
            if (ch4 < 0)
                throw new EndOfStreamException();
            return ch1 + (ch2 << 8) | (ch3 << 16) | (ch4 << 24);
        }

        public override void Close() {
            finish();
            stream.Close();
        }
    }
}
