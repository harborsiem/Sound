/*
 * Copyright (c) 1999, 2018, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.DataInputStream;
//import java.io.IOException;
//import java.io.InputStream;

//import javax.sound.sampled.AudioFileFormat;
//import javax.sound.sampled.spi.AudioFileWriter;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {
/**
 * Abstract File Writer class.
 *
 * @author Jan Borgersen
 */
    public abstract class SunFileWriter : AudioFileWriter {

        // buffer size for write
        protected const int bufferSize = 16384;

        // buffer size for temporary input streams
        protected const int bisBufferSize = 4096;

        protected readonly AudioFileFormat.Type[] types;

        /**
         * Constructs a new SunParser object.
         */
        protected SunFileWriter(AudioFileFormat.Type[] types) {
            this.types = types;
        }

        // METHODS TO IMPLEMENT AudioFileWriter

        public sealed override AudioFileFormat.Type[] getAudioFileTypes() {
            AudioFileFormat.Type[] localArray = new AudioFileFormat.Type[types.Length];
            Array.Copy(types, 0, localArray, 0, types.Length);
            return localArray;
        }

        // HELPER METHODS

        /**
         * rllong
         * Protected helper method to read 64 bits and changing the order of
         * each bytes.
         * @return 32 bits swapped value.
         * @exception IOException
         */
        internal int rllong(BigEndianBinaryReader dis) {

            int b1, b2, b3, b4;
            int i = 0;

            i = dis.ReadInt32();

            b1 = (i & 0xFF) << 24;
            b2 = (i & 0xFF00) << 8;
            b3 = (i & 0xFF0000) >> 8;
            b4 = (int)((uint)(i & 0xFF000000) >> 24);

            i = (b1 | b2 | b3 | b4);

            return i;
        }

        /**
         * big2little
         * Protected helper method to swap the order of bytes in a 32 bit int
         * @return 32 bits swapped value
         */
        internal int big2little(int i) {

            int b1, b2, b3, b4;

            b1 = (i & 0xFF) << 24;
            b2 = (i & 0xFF00) << 8;
            b3 = (i & 0xFF0000) >> 8;
            b4 = (int)((uint)(i & 0xFF000000) >> 24);

            i = (b1 | b2 | b3 | b4);

            return i;
        }

        /**
         * rlshort
         * Protected helper method to read 16 bits value. Swap high with low byte.
         * @return the swapped value.
         * @exception IOException
         */
        internal short rlshort(BigEndianBinaryReader dis) {

            short s = 0;
            short high, low;

            s = dis.ReadInt16();

            high = (short)((s & 0xFF) << 8);
            low = (short)((ushort)(s & 0xFF00) >> 8);

            s = (short)(high | low);

            return s;
        }

        /**
         * big2little
         * Protected helper method to swap the order of bytes in a 16 bit short
         * @return 16 bits swapped value
         */
        internal short big2littleShort(short i) {

            short high, low;

            high = (short)((i & 0xFF) << 8);
            low = (short)((ushort)(i & 0xFF00) >> 8);

            i = (short)(high | low);

            return i;
        }

        /**
         * InputStream wrapper class which prevent source stream from being closed.
         * The class is useful for use with SequenceInputStream to prevent
         * closing of the source input streams.
         */
        internal sealed class NoCloseInputStream : InputStream {
            private readonly InputStream input;

            internal NoCloseInputStream(InputStream input) {
                this.input = input;
            }

            public override int ReadByte() {
                return input.ReadByte();
            }

            public override int Read(byte[] b) {
                return input.Read(b);
            }

            public override int Read(byte[] b, int off, int len) {
                return input.Read(b, off, len);
            }

            public override long skip(long n) {
                return input.skip(n);
            }

            public override int available() {
                return input.available();
            }

            public override void Close() {
                // don't propagate the call
            }

            public override void mark(int readlimit) {
                input.mark(readlimit);
            }

            public override void reset() {
                input.reset();
            }

            public override bool markSupported() {
                return input.markSupported();
            }

            public override long Length {
                get { return input.Length; }
            }

            public override long Position {
                get {
                    return input.Position;
                }
            }
        }

        protected internal static MemoryStream CreateConcatStream(byte[] header, Stream audioStream) { //@ added
            MemoryStream result = new MemoryStream(header.Length + (int)audioStream.Length);
            result.Write(header, 0, header.Length);
            byte[] buffer = new byte[audioStream.Length];
            audioStream.Read(buffer, 0, buffer.Length);
            result.Write(buffer, 0, buffer.Length);
            result.Position = 0;
            return result;
        }
    }
}
