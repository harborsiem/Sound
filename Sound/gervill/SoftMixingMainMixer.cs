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
//import java.util.ArrayList;
//import java.util.List;

//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * Main mixer for SoftMixingMixer.
 * 
 * @author Karl Helgason
 */
    public sealed class SoftMixingMainMixer {

        public const int CHANNEL_LEFT = 0;

        public const int CHANNEL_RIGHT = 1;

        public const int CHANNEL_EFFECT1 = 2;

        public const int CHANNEL_EFFECT2 = 3;

        public const int CHANNEL_EFFECT3 = 4;

        public const int CHANNEL_EFFECT4 = 5;

        public const int CHANNEL_LEFT_DRY = 10;

        public const int CHANNEL_RIGHT_DRY = 11;

        public const int CHANNEL_SCRATCH1 = 12;

        public const int CHANNEL_SCRATCH2 = 13;

        public const int CHANNEL_CHANNELMIXER_LEFT = 14;

        public const int CHANNEL_CHANNELMIXER_RIGHT = 15;

        private readonly SoftMixingMixer mixer;

        private readonly AudioInputStream ais;

        private readonly SoftAudioBuffer[] buffers;

        private readonly ISoftAudioProcessor reverb;

        private readonly ISoftAudioProcessor chorus;

        private readonly ISoftAudioProcessor agc;

        private readonly int nrofchannels;

        private readonly Object control_mutex;

        private readonly List<SoftMixingDataLine> openLinesList = new List<SoftMixingDataLine>();

        private SoftMixingDataLine[] openLines = new SoftMixingDataLine[0];

        public AudioInputStream getInputStream() {
            return ais;
        }

        internal void processAudioBuffers() {
            for (int i = 0; i < buffers.Length; i++) {
                buffers[i].clear();
            }

            SoftMixingDataLine[] openLines;
            lock (control_mutex) {
                openLines = this.openLines;
                for (int i = 0; i < openLines.Length; i++) {
                    openLines[i].processControlLogic();
                }
                chorus.processControlLogic();
                reverb.processControlLogic();
                agc.processControlLogic();
            }
            for (int i = 0; i < openLines.Length; i++) {
                openLines[i].processAudioLogic(buffers);
            }

            chorus.processAudio();
            reverb.processAudio();

            agc.processAudio();
        }

        public SoftMixingMainMixer(SoftMixingMixer mixer) {
            this.mixer = mixer;

            nrofchannels = mixer.getFormat().getChannels();

            int buffersize = (int)(mixer.getFormat().getSampleRate() / mixer
                    .getControlRate());

            control_mutex = mixer.control_mutex;
            buffers = new SoftAudioBuffer[16];
            for (int i = 0; i < buffers.Length; i++) {
                buffers[i] = new SoftAudioBuffer(buffersize, mixer.getFormat());
            }

            reverb = new SoftReverb();
            chorus = new SoftChorus();
            agc = new SoftLimiter();

            float samplerate = mixer.getFormat().getSampleRate();
            float controlrate = mixer.getControlRate();
            reverb.init(samplerate, controlrate);
            chorus.init(samplerate, controlrate);
            agc.init(samplerate, controlrate);

            reverb.setMixMode(true);
            chorus.setMixMode(true);
            agc.setMixMode(false);

            chorus.setInput(0, buffers[CHANNEL_EFFECT2]);
            chorus.setOutput(0, buffers[CHANNEL_LEFT]);
            if (nrofchannels != 1)
                chorus.setOutput(1, buffers[CHANNEL_RIGHT]);
            chorus.setOutput(2, buffers[CHANNEL_EFFECT1]);

            reverb.setInput(0, buffers[CHANNEL_EFFECT1]);
            reverb.setOutput(0, buffers[CHANNEL_LEFT]);
            if (nrofchannels != 1)
                reverb.setOutput(1, buffers[CHANNEL_RIGHT]);

            agc.setInput(0, buffers[CHANNEL_LEFT]);
            if (nrofchannels != 1)
                agc.setInput(1, buffers[CHANNEL_RIGHT]);
            agc.setOutput(0, buffers[CHANNEL_LEFT]);
            if (nrofchannels != 1)
                agc.setOutput(1, buffers[CHANNEL_RIGHT]);

            InputStream input = new InputStreamImpl(this);

            ais = new AudioInputStream(input, mixer.getFormat(),
                    AudioSystem.NOT_SPECIFIED);
        }

        private class InputStreamImpl : InputStream {
            SoftMixingMainMixer caller;
            private readonly SoftAudioBuffer[] buffers;

            private readonly int nrofchannels;

            private readonly int buffersize;

            private readonly byte[] bbuffer;

            private int bbuffer_pos = 0;

            private readonly byte[] single = new byte[1];

            internal InputStreamImpl(SoftMixingMainMixer caller) {
                this.caller = caller;
                buffers = caller.buffers;
                nrofchannels = caller.mixer
                    .getFormat().getChannels();
                buffersize = buffers[0].getSize();
                bbuffer = new byte[buffersize
                    * (caller.mixer.getFormat()
                            .getSampleSizeInBits() / 8) * nrofchannels];

            }

            public override long Position {
                get { return bbuffer_pos; }
            }

            public override long Length {
                get { return bbuffer.Length; }
            }

            public void fillBuffer() {
                caller.processAudioBuffers();
                for (int i = 0; i < nrofchannels; i++)
                    buffers[i].get(bbuffer, i);
                bbuffer_pos = 0;
            }

            public override int Read(byte[] b, int off, int len) {
                int bbuffer_len = this.bbuffer.Length;
                int offlen = off + len;
                byte[] bbuffer = this.bbuffer;
                while (off < offlen)
                    if (available() == 0)
                        fillBuffer();
                    else {
                        int bbuffer_pos = this.bbuffer_pos;
                        while (off < offlen && bbuffer_pos < bbuffer_len)
                            b[off++] = bbuffer[bbuffer_pos++];
                        this.bbuffer_pos = bbuffer_pos;
                    }
                return len;
            }

            public override int ReadByte() {
                int ret = Read(single, 0, single.Length);
                if (ret <= 0)
                    return -1;
                return single[0] & 0xFF;
            }

            public override int available() {
                return bbuffer.Length - bbuffer_pos;
            }

            public override void Close() {
                caller.mixer.close();
            }
        }

        public void openLine(SoftMixingDataLine line) {
            lock (control_mutex) {
                openLinesList.Add(line);
                openLines = openLinesList.ToArray();
            }
        }

        public void closeLine(SoftMixingDataLine line) {
            lock (control_mutex) {
                openLinesList.Remove(line);
                openLines = openLinesList.ToArray();
                if (openLines.Length == 0)
                    if (mixer.implicitOpen)
                        mixer.close();
            }
        }

        public SoftMixingDataLine[] getOpenLines() {
            lock (control_mutex) {
                return openLines;
            }
        }

        public void close() {
            SoftMixingDataLine[] openLines = this.openLines;
            for (int i = 0; i < openLines.Length; i++) {
                openLines[i].close();
            }
        }
    }
}
