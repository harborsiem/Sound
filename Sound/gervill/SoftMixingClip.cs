/*
 * Copyright (c) 2008, 2016, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.ByteArrayOutputStream;
//import java.io.IOException;
//import java.io.InputStream;
//import java.util.Arrays;

//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.Clip;
//import javax.sound.sampled.DataLine;
//import javax.sound.sampled.LineEvent;
//import javax.sound.sampled.LineUnavailableException;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * Clip implementation for the SoftMixingMixer.
 * 
 * @author Karl Helgason
 */
    public sealed class SoftMixingClip : SoftMixingDataLine, IClip {

        private AudioFormat format;

        private int framesize;

        private byte[] data;

        private readonly InputStream datastream; // = new InputStreamImpl();

        private int offset;

        private int bufferSize;

        private float[] readbuffer;

        private bool _open = false;

        private AudioFormat outputformat;

        private int out_nrofchannels;

        private int in_nrofchannels;

        private int frameposition = 0;

        private bool frameposition_sg = false;

        private bool active_sg = false;

        private int loopstart = 0;

        private int loopend = -1;

        private bool active = false;

        private int loopcount = 0;

        private bool _active = false;

        private int _frameposition = 0;

        private bool loop_sg = false;

        private int _loopcount = 0;

        private int _loopstart = 0;

        private int _loopend = -1;

        private float _rightgain;

        private float _leftgain;

        private float _eff1gain;

        private float _eff2gain;

        private AudioFloatInputStream afis;

        internal SoftMixingClip(SoftMixingMixer mixer, DataLine.Info info)
            : base(mixer, info) {
            datastream = new InputStreamImpl(this);
        }

        protected internal override void processControlLogic() {
            _rightgain = rightgain;
            _leftgain = leftgain;
            _eff1gain = eff1gain;
            _eff2gain = eff2gain;

            if (active_sg) {
                _active = active;
                active_sg = false;
            } else {
                active = _active;
            }

            if (frameposition_sg) {
                _frameposition = frameposition;
                frameposition_sg = false;
                afis = null;
            } else {
                frameposition = _frameposition;
            }
            if (loop_sg) {
                _loopcount = loopcount;
                _loopstart = loopstart;
                _loopend = loopend;
            }

            if (afis == null) {
                afis = AudioFloatInputStream.getInputStream(new AudioInputStream(
                        datastream, format, AudioSystem.NOT_SPECIFIED));

                if (Math.Abs(format.getSampleRate() - outputformat.getSampleRate()) > 0.000001)
                    afis = new AudioFloatInputStreamResampler(afis, outputformat);
            }
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
                    if (ret == -1) {
                        _active = false;
                        return;
                    }
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

                if (_eff1gain > 0.0002) {

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

                if (_eff2gain > 0.0002) {
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

        public int getFrameLength() {
            return bufferSize / format.getFrameSize();
        }

        public long getMicrosecondLength() {
            return (long)(getFrameLength() * (1000000.0 / (double)getFormat()
                    .getSampleRate()));
        }

        public void loop(int count) {
            LineEvent evnt = null;

            lock (control_mutex) {
                if (isOpen()) {
                    if (active)
                        return;
                    active = true;
                    active_sg = true;
                    loopcount = count;
                    evnt = new LineEvent(this, LineEvent.Type.START,
                            getLongFramePosition());
                }
            }

            if (evnt != null)
                sendEvent(evnt);
        }

        public void open(AudioInputStream stream) {
            if (isOpen()) {
                throw new InvalidOperationException("Clip is already open with format "
                        + getFormat() + " and frame length of " + getFrameLength());
            }
            if (AudioFloatConverter.getConverter(stream.getFormat()) == null)
                throw new ArgumentException("Invalid format : "
                        + stream.getFormat().ToString());

            if (stream.getFrameLength() != AudioSystem.NOT_SPECIFIED) {
                byte[] data = new byte[(int)stream.getFrameLength()
                        * stream.getFormat().getFrameSize()];
                int readsize = 512 * stream.getFormat().getFrameSize();
                int len = 0;
                while (len != data.Length) {
                    if (readsize > data.Length - len)
                        readsize = data.Length - len;
                    int ret = stream.Read(data, len, readsize);
                    if (ret == -1)
                        break;
                    if (ret == 0)
                        //Thread.Sleep(0);
                        Thread.Yield();
                    len += ret;
                }
                open(stream.getFormat(), data, 0, len);
            } else {
                MemoryStream baos = new MemoryStream();
                byte[] b = new byte[512 * stream.getFormat().getFrameSize()];
                int r = 0;
                while ((r = stream.Read(b, 0, b.Length)) > 0) {
                    if (r == 0)
                        //Thread.Sleep(0);
                        Thread.Yield();
                    baos.Write(b, 0, r);
                }
                open(stream.getFormat(), baos.ToArray(), 0, (int)baos.Length);
            }
        }

        public void open(AudioFormat format, byte[] data, int offset, int bufferSize) {
            lock (control_mutex) {
                if (isOpen()) {
                    throw new InvalidOperationException(
                            "Clip is already open with format " + getFormat()
                                    + " and frame length of " + getFrameLength());
                }
                if (AudioFloatConverter.getConverter(format) == null)
                    throw new ArgumentException("Invalid format : "
                            + format.ToString());
                Toolkit.validateBuffer(format.getFrameSize(), bufferSize);

                if (data != null) {
                    byte[] temp = new byte[data.Length];
                    Array.Copy(data, temp, data.Length);
                    this.data = temp;
                }
                this.offset = offset;
                this.bufferSize = bufferSize;
                this.format = format;
                this.framesize = format.getFrameSize();

                loopstart = 0;
                loopend = -1;
                loop_sg = true;

                if (!mixer.isOpen()) {
                    mixer.open();
                    mixer.implicitOpen = true;
                }

                outputformat = mixer.getFormat();
                out_nrofchannels = outputformat.getChannels();
                in_nrofchannels = format.getChannels();

                _open = true;

                mixer.getMainMixer().openLine(this);
            }
        }

        public void setFramePosition(int frames) {
            lock (control_mutex) {
                frameposition_sg = true;
                frameposition = frames;
            }
        }

        public void setLoopPoints(int start, int end) {
            lock (control_mutex) {
                if (end != -1) {
                    if (end < start)
                        throw new ArgumentException("Invalid loop points : "
                                + start + " - " + end);
                    if (end * framesize > bufferSize)
                        throw new ArgumentException("Invalid loop points : "
                                + start + " - " + end);
                }
                if (start * framesize > bufferSize)
                    throw new ArgumentException("Invalid loop points : "
                            + start + " - " + end);
                if (0 < start)
                    throw new ArgumentException("Invalid loop points : "
                            + start + " - " + end);
                loopstart = start;
                loopend = end;
                loop_sg = true;
            }
        }

        public void setMicrosecondPosition(long microseconds) {
            setFramePosition((int)(microseconds * (((double)getFormat()
                    .getSampleRate()) / 1000000.0)));
        }

        public override int available() {
            return 0;
        }

        public override void drain() {
        }

        public override void flush() {
        }

        public override int getBufferSize() {
            return bufferSize;
        }

        public override AudioFormat getFormat() {
            return format;
        }

        public override int getFramePosition() {
            lock (control_mutex) {
                return frameposition;
            }
        }

        public override float getLevel() {
            return AudioSystem.NOT_SPECIFIED;
        }

        public override long getLongFramePosition() {
            return getFramePosition();
        }

        public override long getMicrosecondPosition() {
            return (long)(getFramePosition() * (1000000.0 / (double)getFormat()
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
                    active_sg = true;
                    loopcount = 0;
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
                    active_sg = true;
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
            return _open;
        }

        public override void open() {
            if (data == null) {
                throw new ArgumentException(
                        "Illegal call to open() in interface Clip");
            }
            open(format, data, offset, bufferSize);
        }

        private class InputStreamImpl : InputStream {
            SoftMixingClip caller;

            public InputStreamImpl(SoftMixingClip caller) {
                this.caller = caller;
            }

            public override long Position {
                get { return caller._frameposition * caller.framesize; }
            }

            public override long Length {
                get { return caller.data.Length; }
            }

            public override int ReadByte() {
                byte[] b = new byte[1];
                int ret = Read(b, 0, b.Length);
                if (ret <= 0)
                    return -1;
                return b[0] & 0xFF;
            }

            public override int Read(byte[] b, int off, int len) {
                int pos;
                int left;

                if (caller._loopcount != 0) {
                    int bloopend = caller._loopend * caller.framesize;
                    int bloopstart = caller._loopstart * caller.framesize;
                    pos = caller._frameposition * caller.framesize;

                    if (pos + len >= bloopend)
                        if (pos < bloopend) {
                            int offend = off + len;
                            int o = off;
                            while (off != offend) {
                                if (pos == bloopend) {
                                    if (caller._loopcount == 0)
                                        break;
                                    pos = bloopstart;
                                    if (caller._loopcount != Clip.LOOP_CONTINUOUSLY)
                                        caller._loopcount--;
                                }
                                len = offend - off;
                                left = bloopend - pos;
                                if (len > left)
                                    len = left;
                                Array.Copy(caller.data, pos, b, off, len);
                                off += len;
                            }
                            if (caller._loopcount == 0) {
                                len = offend - off;
                                left = bloopend - pos;
                                if (len > left)
                                    len = left;
                                Array.Copy(caller.data, pos, b, off, len);
                                off += len;
                            }
                            caller._frameposition = pos / caller.framesize;
                            return o - off;
                        }
                }

                pos = caller._frameposition * caller.framesize;
                left = caller.bufferSize - pos;
                if (left == 0)
                    return -1;
                if (len > left)
                    len = left;
                Array.Copy(caller.data, pos, b, off, len);
                caller._frameposition += len / caller.framesize;
                return len;
            }

            public override int available() {
                return 0;
            }
        }
    }
}
