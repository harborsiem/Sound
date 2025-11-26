/*
 * Copyright (c) 2008, 2013, Oracle and/or its affiliates. All rights reserved.
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
//import java.util.Arrays;

//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.DataLine;
//import javax.sound.sampled.LineEvent;
//import javax.sound.sampled.LineUnavailableException;
//import javax.sound.sampled.SourceDataLine;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * SourceDataLine implementation for the SoftMixingMixer.
 * 
 * @author Karl Helgason
 */
    public sealed class SoftMixingSourceDataLine : SoftMixingDataLine, ISourceDataLine {

        private bool _open = false;

        private AudioFormat format = new AudioFormat(44100.0f, 16, 2, true, false);

        private int framesize;

        private int bufferSize = -1;

        private float[] readbuffer;

        private bool active = false;

        private byte[] cycling_buffer;

        private int cycling_read_pos = 0;

        private int cycling_write_pos = 0;

        private int cycling_avail = 0;

        private long cycling_framepos = 0;

        private AudioFloatInputStream afis;

        private class NonBlockingFloatInputStream :
                AudioFloatInputStream {
            AudioFloatInputStream ais;

            internal NonBlockingFloatInputStream(AudioFloatInputStream ais) {
                this.ais = ais;
            }

            public override int available() {
                return ais.available();
            }

            public override void close() {
                ais.close();
            }

            public override AudioFormat getFormat() {
                return ais.getFormat();
            }

            public override long getFrameLength() {
                return ais.getFrameLength();
            }

            public override void mark(int readlimit) {
                ais.mark(readlimit);
            }

            public override bool markSupported() {
                return ais.markSupported();
            }

            public override int read(float[] b, int off, int len) {
                int avail = available();
                if (len > avail) {
                    int ret = ais.read(b, off, avail);
                    Array.Clear(b, off + ret, off + len - (off + ret));
                    return len;
                }
                return ais.read(b, off, len);
            }

            public override void reset() {
                ais.reset();
            }

            public override long skip(long len) {
                return ais.skip(len);
            }

        }

        internal SoftMixingSourceDataLine(SoftMixingMixer mixer, DataLine.Info info)
            : base(mixer, info) {
        }

        public int write(byte[] b, int off, int len) {
            if (!isOpen())
                return 0;
            if (len % framesize != 0)
                throw new ArgumentException(
                        "Number of bytes does not represent an integral number of sample frames.");
            if (off < 0) {
                throw new ArgumentException(off.ToString(CultureInfo.InvariantCulture));
            }
            if ((long)off + (long)len > (long)b.Length) {
                throw new ArgumentException(b.Length.ToString(CultureInfo.InvariantCulture));
            }

            byte[] buff = cycling_buffer;
            int buff_len = cycling_buffer.Length;

            int l = 0;
            while (l != len) {
                int avail;
                lock (cycling_buffer) {
                    int pos = cycling_write_pos;
                    avail = cycling_avail;
                    while (l != len) {
                        if (avail == buff_len)
                            break;
                        buff[pos++] = b[off++];
                        l++;
                        avail++;
                        if (pos == buff_len)
                            pos = 0;
                    }
                    cycling_avail = avail;
                    cycling_write_pos = pos;
                    if (l == len)
                        return l;
                }
                if (avail == buff_len) {
                    try {
                        Thread.Sleep(1);
                    }
                    catch (ThreadInterruptedException) {
                        return l;
                    }
                    if (!isRunning())
                        return l;
                }
            }

            return l;
        }

        //
        // BooleanControl.Type.APPLY_REVERB
        // BooleanControl.Type.MUTE
        // EnumControl.Type.REVERB
        //
        // FloatControl.Type.SAMPLE_RATE
        // FloatControl.Type.REVERB_SEND
        // FloatControl.Type.VOLUME
        // FloatControl.Type.PAN
        // FloatControl.Type.MASTER_GAIN
        // FloatControl.Type.BALANCE

        private bool _active = false;

        private AudioFormat outputformat;

        private int out_nrofchannels;

        private int in_nrofchannels;

        private float _rightgain;

        private float _leftgain;

        private float _eff1gain;

        private float _eff2gain;

        protected internal override void processControlLogic() {
            _active = active;
            _rightgain = rightgain;
            _leftgain = leftgain;
            _eff1gain = eff1gain;
            _eff2gain = eff2gain;
        }

        protected internal override void processAudioLogic(SoftAudioBuffer[] buffers) {
            if (_active) {
                float[] left = buffers[SoftMixingMainMixer.CHANNEL_LEFT].array();
                float[] right = buffers[SoftMixingMainMixer.CHANNEL_RIGHT].array();
                int bufferlen = buffers[SoftMixingMainMixer.CHANNEL_LEFT].getSize();

                int readlen = bufferlen * in_nrofchannels;
                if (readbuffer == null || readbuffer.Length < readlen) {
                    readbuffer = new float[readlen];
                }
                int ret = 0;
                try {
                    ret = afis.read(readbuffer);
                    if (ret != in_nrofchannels)
                        Array.Clear(readbuffer, ret, readlen - ret);
                }
                catch (IOException) {
                }

                int in_c = in_nrofchannels;
                for (int i = 0, ix = 0; i < bufferlen; i++, ix += in_c) {
                    left[i] += readbuffer[ix] * _leftgain;
                }
                if (out_nrofchannels != 1) {
                    if (in_nrofchannels == 1) {
                        for (int i = 0, ix = 0; i < bufferlen; i++, ix += in_c) {
                            right[i] += readbuffer[ix] * _rightgain;
                        }
                    } else {
                        for (int i = 0, ix = 1; i < bufferlen; i++, ix += in_c) {
                            right[i] += readbuffer[ix] * _rightgain;
                        }
                    }

                }

                if (_eff1gain > 0.0001) {
                    float[] eff1 = buffers[SoftMixingMainMixer.CHANNEL_EFFECT1]
                            .array();
                    for (int i = 0, ix = 0; i < bufferlen; i++, ix += in_c) {
                        eff1[i] += readbuffer[ix] * _eff1gain;
                    }
                    if (in_nrofchannels == 2) {
                        for (int i = 0, ix = 1; i < bufferlen; i++, ix += in_c) {
                            eff1[i] += readbuffer[ix] * _eff1gain;
                        }
                    }
                }

                if (_eff2gain > 0.0001) {
                    float[] eff2 = buffers[SoftMixingMainMixer.CHANNEL_EFFECT2]
                            .array();
                    for (int i = 0, ix = 0; i < bufferlen; i++, ix += in_c) {
                        eff2[i] += readbuffer[ix] * _eff2gain;
                    }
                    if (in_nrofchannels == 2) {
                        for (int i = 0, ix = 1; i < bufferlen; i++, ix += in_c) {
                            eff2[i] += readbuffer[ix] * _eff2gain;
                        }
                    }
                }

            }
        }

        public override void open() {
            open(format);
        }

        public void open(AudioFormat format) {
            if (bufferSize == -1)
                bufferSize = ((int)(format.getFrameRate() / 2))
                        * format.getFrameSize();
            open(format, bufferSize);
        }

        public void open(AudioFormat format, int bufferSize) {

            LineEvent evnt = null;

            if (bufferSize < format.getFrameSize() * 32)
                bufferSize = format.getFrameSize() * 32;

            lock (control_mutex) {

                if (!isOpen()) {
                    if (!mixer.isOpen()) {
                        mixer.open();
                        mixer.implicitOpen = true;
                    }

                    evnt = new LineEvent(this, LineEvent.Type.OPEN, 0);

                    this.bufferSize = bufferSize - bufferSize
                            % format.getFrameSize();
                    this.format = format;
                    this.framesize = format.getFrameSize();
                    this.outputformat = mixer.getFormat();
                    out_nrofchannels = outputformat.getChannels();
                    in_nrofchannels = format.getChannels();

                    _open = true;

                    mixer.getMainMixer().openLine(this);

                    cycling_buffer = new byte[framesize * bufferSize];
                    cycling_read_pos = 0;
                    cycling_write_pos = 0;
                    cycling_avail = 0;
                    cycling_framepos = 0;

                    InputStream cycling_inputstream = new InputStreamImpl(this);
                    afis = AudioFloatInputStream
                            .getInputStream(new AudioInputStream(
                                    cycling_inputstream, format,
                                    AudioSystem.NOT_SPECIFIED));
                    afis = new NonBlockingFloatInputStream(afis);

                    if (Math.Abs(format.getSampleRate()
                            - outputformat.getSampleRate()) > 0.000001)
                        afis = new AudioFloatInputStreamResampler(afis,
                                outputformat);

                } else {
                    if (!format.matches(getFormat())) {
                        throw new InvalidOperationException(
                                "Line is already open with format " + getFormat()
                                        + " and bufferSize " + getBufferSize());
                    }
                }

            }

            if (evnt != null)
                sendEvent(evnt);

        }

        public override int available() {
            lock (cycling_buffer) {
                return cycling_buffer.Length - cycling_avail;
            }
        }

        public override void drain() {
            while (true) {
                int avail;
                lock (cycling_buffer) {
                    avail = cycling_avail;
                }
                if (avail != 0)
                    return;
                try {
                    Thread.Sleep(1);
                }
                catch (ThreadInterruptedException) {
                    return;
                }
            }
        }

        public override void flush() {
            lock (cycling_buffer) {
                cycling_read_pos = 0;
                cycling_write_pos = 0;
                cycling_avail = 0;
            }
        }

        public override int getBufferSize() {
            lock (control_mutex) {
                return bufferSize;
            }
        }

        public override AudioFormat getFormat() {
            lock (control_mutex) {
                return format;
            }
        }

        public override int getFramePosition() {
            return (int)getLongFramePosition();
        }

        public override float getLevel() {
            return AudioSystem.NOT_SPECIFIED;
        }

        public override long getLongFramePosition() {
            lock (cycling_buffer) {
                return cycling_framepos;
            }
        }

        public override long getMicrosecondPosition() {
            return (long)(getLongFramePosition() * (1000000.0 / (double)getFormat()
                    .getSampleRate()));
        }

        public override bool isActive() {
            lock (control_mutex) {
                return active;
            }
        }

        public override bool isRunning() {
            lock (control_mutex) {
                return active;
            }
        }

        public override void start() {

            LineEvent evnt = null;

            lock (control_mutex) {
                if (isOpen()) {
                    if (active)
                        return;
                    active = true;
                    evnt = new LineEvent(this, LineEvent.Type.START,
                            getLongFramePosition());
                }
            }

            if (evnt != null)
                sendEvent(evnt);
        }

        public override void stop() {
            LineEvent evnt = null;

            lock (control_mutex) {
                if (isOpen()) {
                    if (!active)
                        return;
                    active = false;
                    evnt = new LineEvent(this, LineEvent.Type.STOP,
                            getLongFramePosition());
                }
            }

            if (evnt != null)
                sendEvent(evnt);
        }

        public override void close() {

            LineEvent evnt = null;

            lock (control_mutex) {
                if (!isOpen())
                    return;
                stop();

                evnt = new LineEvent(this, LineEvent.Type.CLOSE,
                        getLongFramePosition());

                _open = false;
                mixer.getMainMixer().closeLine(this);
            }

            if (evnt != null)
                sendEvent(evnt);
        }

        public override bool isOpen() {
            lock (control_mutex) {
                return _open;
            }
        }

        private class InputStreamImpl : InputStream {
            SoftMixingSourceDataLine caller;

            public InputStreamImpl(SoftMixingSourceDataLine caller) {
                this.caller = caller;
            }

            public override long Position {
                get { return caller.cycling_read_pos; }
            }

            public override long Length {
                get { return caller.cycling_buffer.Length; }
            }

            public override int ReadByte() {
                byte[] b = new byte[1];
                int ret = Read(b, 0, b.Length);
                if (ret <= 0)
                    return -1;
                return b[0] & 0xFF;
            }

            public override int available() {
                lock (caller.cycling_buffer) {
                    return caller.cycling_avail;
                }
            }

            public override int Read(byte[] b, int off, int len) {

                lock (caller.cycling_buffer) {
                    if (len > caller.cycling_avail)
                        len = caller.cycling_avail;
                    int pos = caller.cycling_read_pos;
                    byte[] buff = caller.cycling_buffer;
                    int buff_len = buff.Length;
                    for (int i = 0; i < len; i++) {
                        b[off++] = buff[pos];
                        pos++;
                        if (pos == buff_len)
                            pos = 0;
                    }
                    caller.cycling_read_pos = pos;
                    caller.cycling_avail -= len;
                    caller.cycling_framepos += len / caller.framesize;
                }
                return len;
            }
        }
    }
}