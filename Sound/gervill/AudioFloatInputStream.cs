/*
 * Copyright (c) 2007, 2013, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.ByteArrayInputStream;
//import java.io.File;
//import java.io.IOException;
//import java.io.InputStream;
//import java.net.URL;
//import java.util.Objects;

//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.UnsupportedAudioFileException;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * This class is used to create AudioFloatInputStream from AudioInputStream and
 * byte buffers.
 *
 * @author Karl Helgason
 */
    public abstract class AudioFloatInputStream {

        private class BytaArrayAudioFloatInputStream
                : AudioFloatInputStream {

            private int pos = 0;
            private int markpos = 0;
            private readonly AudioFloatConverter converter;
            private readonly AudioFormat format;
            private readonly byte[] buffer;
            private readonly int buffer_offset;
            private readonly int buffer_len;
            private readonly int framesize_pc;

            internal BytaArrayAudioFloatInputStream(AudioFloatConverter converter,
                    byte[] buffer, int offset, int len) {
                this.converter = converter;
                this.format = converter.getFormat();
                this.buffer = buffer;
                this.buffer_offset = offset;
                framesize_pc = format.getFrameSize() / format.getChannels();
                this.buffer_len = len / framesize_pc;

            }

            public override AudioFormat getFormat() {
                return format;
            }

            public override long getFrameLength() {
                return buffer_len;// / format.getFrameSize();
            }

            public override int read(float[] b, int off, int len) {
                if (b == null)
                    throw new ArgumentNullException(nameof(b));
                if (off < 0 || len < 0 || len > b.Length - off)
                    throw new IndexOutOfRangeException("Parameter off or len are out of range");
                if (pos >= buffer_len)
                    return -1;
                if (len == 0)
                    return 0;
                if (pos + len > buffer_len)
                    len = buffer_len - pos;
                converter.toFloatArray(buffer, buffer_offset + pos * framesize_pc,
                        b, off, len);
                pos += len;
                return len;
            }

            public override long skip(long len) {
                if (pos >= buffer_len)
                    return -1;
                if (len <= 0)
                    return 0;
                if (pos + len > buffer_len)
                    len = buffer_len - pos;
                pos += (int)len;
                return len;
            }

            public override int available() {
                return buffer_len - pos;
            }

            public override void close() {
            }

            public override void mark(int readlimit) {
                markpos = pos;
            }

            public override bool markSupported() {
                return true;
            }

            public override void reset() {
                pos = markpos;
            }
        }

        private class DirectAudioFloatInputStream
                : AudioFloatInputStream {

            private readonly AudioInputStream stream;
            private AudioFloatConverter converter;
            private readonly int framesize_pc; // framesize / channels
            private byte[] buffer;

            internal DirectAudioFloatInputStream(AudioInputStream stream) {
                converter = AudioFloatConverter.getConverter(stream.getFormat());
                if (converter == null) {
                    AudioFormat format = stream.getFormat();
                    AudioFormat newformat;

                    AudioFormat[] formats = AudioSystem.getTargetFormats(
                            AudioFormat.Encoding.PCM_SIGNED, format);
                    if (formats.Length != 0) {
                        newformat = formats[0];
                    } else {
                        float samplerate = format.getSampleRate();
                        int samplesizeinbits = format.getSampleSizeInBits();
                        int framesize = format.getFrameSize();
                        float framerate = format.getFrameRate();
                        samplesizeinbits = 16;
                        framesize = format.getChannels() * (samplesizeinbits / 8);
                        framerate = samplerate;

                        newformat = new AudioFormat(
                                AudioFormat.Encoding.PCM_SIGNED, samplerate,
                                samplesizeinbits, format.getChannels(), framesize,
                                framerate, false);
                    }

                    stream = AudioSystem.getAudioInputStream(newformat, stream);
                    converter = AudioFloatConverter.getConverter(stream.getFormat());
                }
                framesize_pc = stream.getFormat().getFrameSize()
                        / stream.getFormat().getChannels();
                this.stream = stream;
            }

            public override AudioFormat getFormat() {
                return stream.getFormat();
            }

            public override long getFrameLength() {
                return stream.getFrameLength();
            }

            public override int read(float[] b, int off, int len) {
                int b_len = len * framesize_pc;
                if (buffer == null || buffer.Length < b_len)
                    buffer = new byte[b_len];
                int ret = stream.Read(buffer, 0, b_len);
                if (ret <= 0)
                    return 0;
                converter.toFloatArray(buffer, b, off, ret / framesize_pc);
                return ret / framesize_pc;
            }

            public override long skip(long len) {
                long b_len = len * framesize_pc;
                long ret = stream.skip(b_len);
                if (ret <= 0)
                    return -1;
                return ret / framesize_pc;
            }

            public override int available() {
                return (int)(stream.available() / framesize_pc);
            }

            public override void close() {
                stream.Close();
            }

            public override void mark(int readlimit) {
                stream.mark(readlimit * framesize_pc);
            }

            public override bool markSupported() {
                return stream.markSupported();
            }

            public override void reset() {
                stream.reset();
            }
        }

        public static AudioFloatInputStream getInputStream(Uri url) {
            return new DirectAudioFloatInputStream(AudioSystem.getAudioInputStream(url));
        }

        public static AudioFloatInputStream getInputStream(FileInfo file) {
            return new DirectAudioFloatInputStream(AudioSystem.getAudioInputStream(file));
        }

        public static AudioFloatInputStream getInputStream(Stream stream) {
            return new DirectAudioFloatInputStream(AudioSystem.getAudioInputStream(stream));
        }

        public static AudioFloatInputStream getInputStream(
                AudioInputStream stream) {
            return new DirectAudioFloatInputStream(stream);
        }

        public static AudioFloatInputStream getInputStream(AudioFormat format,
                byte[] buffer, int offset, int len) {
            AudioFloatConverter converter = AudioFloatConverter.getConverter(format);
            if (converter != null)
                return new BytaArrayAudioFloatInputStream(converter, buffer,
                        offset, len);

            Stream stream = new MemoryStream(buffer, offset, len);
            long aLen = format.getFrameSize() == AudioSystem.NOT_SPECIFIED
                    ? AudioSystem.NOT_SPECIFIED : len / format.getFrameSize();
            AudioInputStream astream = new AudioInputStream(stream, format, aLen);
            return getInputStream(astream);
        }

        public abstract AudioFormat getFormat();

        public abstract long getFrameLength();

        public abstract int read(float[] b, int off, int len);

        public int read(float[] b) {
            return read(b, 0, b.Length);
        }

        public float read() {
            float[] b = new float[1];
            int ret = read(b, 0, 1);
            if (ret == -1 || ret == 0)
                return 0;
            return b[0];
        }

        public abstract long skip(long len);

        public abstract int available();

        public abstract void close();

        public abstract void mark(int readlimit);

        public abstract bool markSupported();

        public abstract void reset();
    }
}
