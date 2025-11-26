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

//import java.io.IOException;
//import java.io.InputStream;

//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioFormat.Encoding;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * Wavetable oscillator for pre-loaded data.
 *
 * @author Karl Helgason
 */
    public sealed class ModelByteBufferWavetable : IModelWavetable {

        private class Buffer8PlusInputStream : InputStream {
            ModelByteBufferWavetable caller;

            private readonly bool bigendian;
            private readonly int framesize_pc;
            int pos = 0;
            int pos2 = 0;
            int markpos = 0;
            int markpos2 = 0;

            internal Buffer8PlusInputStream(ModelByteBufferWavetable caller) {
                this.caller = caller;
                framesize_pc = caller.format.getFrameSize() / caller.format.getChannels();
                bigendian = caller.format.isBigEndian();
            }

            public override long Position {
                get { return (pos + pos2); }
            }

            public override long Length {
                get { return (int)caller.buffer.capacity() + (int)caller.buffer8.capacity(); }
            }

            public override int Read(byte[] b, int off, int len) {
                int avail = available();
                if (avail <= 0)
                    return -1;
                if (len > avail)
                    len = avail;
                byte[] buff1 = caller.buffer.array();
                byte[] buff2 = caller.buffer8.array();
                pos += (int)caller.buffer.arrayOffset();
                pos2 += (int)caller.buffer8.arrayOffset();
                if (bigendian) {
                    for (int i = 0; i < len; i += (framesize_pc + 1)) {
                        Array.Copy(buff1, pos, b, i, framesize_pc);
                        Array.Copy(buff2, pos2, b, i + framesize_pc, 1);
                        pos += framesize_pc;
                        pos2 += 1;
                    }
                } else {
                    for (int i = 0; i < len; i += (framesize_pc + 1)) {
                        Array.Copy(buff2, pos2, b, i, 1);
                        Array.Copy(buff1, pos, b, i + 1, framesize_pc);
                        pos += framesize_pc;
                        pos2 += 1;
                    }
                }
                pos -= (int)caller.buffer.arrayOffset();
                pos2 -= (int)caller.buffer8.arrayOffset();
                return len;
            }

            public override long skip(long n) {
                int avail = available();
                if (avail <= 0)
                    return -1;
                if (n > avail)
                    n = avail;
                pos += (int)((n / (framesize_pc + 1)) * (framesize_pc));
                pos2 += (int)(n / (framesize_pc + 1));
                return base.skip(n);
            }

            public int read(byte[] b) {
                return Read(b, 0, b.Length);
            }

            public override int ReadByte() {
                byte[] b = new byte[1];
                int ret = Read(b, 0, 1);
                if (ret <= 0)
                    return -1;
                return 0 & 0xFF;
            }

            public override bool markSupported() {
                return true;
            }

            public override int available() {
                return (int)caller.buffer.capacity() + (int)caller.buffer8.capacity() - pos - pos2;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void mark(int readlimit) {
                markpos = pos;
                markpos2 = pos2;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void reset() {
                pos = markpos;
                pos2 = markpos2;

            }
        }

        private float loopStart = -1;
        private float loopLength = -1;
        private readonly ModelByteBuffer buffer;
        private ModelByteBuffer buffer8 = null;
        private AudioFormat format = null;
        private float pitchcorrection = 0;
        private float attenuation = 0;
        private int loopType = ModelWavetable.LOOP_TYPE_OFF;

        public ModelByteBufferWavetable(ModelByteBuffer buffer) {
            this.buffer = buffer;
        }

        public ModelByteBufferWavetable(ModelByteBuffer buffer,
                float pitchcorrection) {
            this.buffer = buffer;
            this.pitchcorrection = pitchcorrection;
        }

        public ModelByteBufferWavetable(ModelByteBuffer buffer, AudioFormat format) {
            this.format = format;
            this.buffer = buffer;
        }

        public ModelByteBufferWavetable(ModelByteBuffer buffer, AudioFormat format,
                float pitchcorrection) {
            this.format = format;
            this.buffer = buffer;
            this.pitchcorrection = pitchcorrection;
        }

        public void set8BitExtensionBuffer(ModelByteBuffer buffer) {
            buffer8 = buffer;
        }

        public ModelByteBuffer get8BitExtensionBuffer() {
            return buffer8;
        }

        public ModelByteBuffer getBuffer() {
            return buffer;
        }

        public AudioFormat getFormat() {
            if (this.format == null) {
                if (buffer == null)
                    return null;
                AudioFormat format = null;
                try {
                    using (Stream istream = buffer.getInputStream()) {
                        format = AudioSystem.getAudioFileFormat(istream).getFormat();
                    }
                }
                catch (Exception e) {
                    Printer.printStackTrace(e);
                }
                return format;
            }
            return this.format;
        }

        public AudioFloatInputStream openStream() {
            if (buffer == null)
                return null;
            if (format == null) {
                Stream istream = buffer.getInputStream();
                AudioInputStream ais = null;
                try {
                    ais = AudioSystem.getAudioInputStream(istream);
                }
                catch (Exception e) {
                    Printer.printStackTrace(e);
                    return null;
                }
                return AudioFloatInputStream.getInputStream(ais);
            }
            if (buffer.array() == null) {
                return AudioFloatInputStream.getInputStream(new AudioInputStream(
                    buffer.getInputStream(), format,
                    buffer.capacity() / format.getFrameSize()));
            }
            if (buffer8 != null) {
                if (format.getEncoding().Equals(AudioFormat.Encoding.PCM_SIGNED)
                        || format.getEncoding().Equals(AudioFormat.Encoding.PCM_UNSIGNED)) {
                    InputStream istream = new Buffer8PlusInputStream(this);
                    AudioFormat format2 = new AudioFormat(
                            format.getEncoding(),
                            format.getSampleRate(),
                            format.getSampleSizeInBits() + 8,
                            format.getChannels(),
                            format.getFrameSize() + (1 * format.getChannels()),
                            format.getFrameRate(),
                            format.isBigEndian());

                    AudioInputStream ais = new AudioInputStream(istream, format2,
                            buffer.capacity() / format.getFrameSize());
                    return AudioFloatInputStream.getInputStream(ais);
                }
            }
            return AudioFloatInputStream.getInputStream(format, buffer.array(),
                    (int)buffer.arrayOffset(), (int)buffer.capacity());
        }

        public int getChannels() {
            return getFormat().getChannels();
        }

        public IModelOscillatorStream open(float samplerate) {
            // ModelWavetableOscillator doesn't support ModelOscillatorStream
            return null;
        }

        // attenuation is in cB
        public float getAttenuation() {
            return attenuation;
        }
        // attenuation is in cB
        public void setAttenuation(float attenuation) {
            this.attenuation = attenuation;
        }

        public float getLoopLength() {
            return loopLength;
        }

        public void setLoopLength(float loopLength) {
            this.loopLength = loopLength;
        }

        public float getLoopStart() {
            return loopStart;
        }

        public void setLoopStart(float loopStart) {
            this.loopStart = loopStart;
        }

        public void setLoopType(int loopType) {
            this.loopType = loopType;
        }

        public int getLoopType() {
            return loopType;
        }

        public float getPitchcorrection() {
            return pitchcorrection;
        }

        public void setPitchcorrection(float pitchcorrection) {
            this.pitchcorrection = pitchcorrection;
        }
    }
}
