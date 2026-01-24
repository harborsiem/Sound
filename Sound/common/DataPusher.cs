/*
 * Copyright (c) 2002, 2025, Oracle and/or its affiliates. All rights reserved.
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

//import java.util.Arrays;

//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.SourceDataLine;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using SystemX.Addon;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * Class to write an AudioInputStream to a SourceDataLine.
 * Was previously an inner class in various classes like JavaSoundAudioClip
 * and sun.audio.AudioDevice.
 * It auto-opens and closes the SourceDataLine.
 *
 * @author Kara Kytle
 * @author Florian Bomers
 */
    public sealed class DataPusher : IRunnable {

        private const int AUTO_CLOSE_TIME = 5000;

        private readonly ISourceDataLine source;
        private readonly AudioFormat format;

        // stream as source data
        private readonly AudioInputStream ais;

        // byte array as source data
        private readonly byte[] audioData;
        private readonly int audioDataByteLength;
        private int pos;
        private int newPos = -1;
        private bool looping;

        private Thread pushThread = null;
        bool daemonThread = false;
        private int wantedState;
        private int threadState;

#pragma warning disable 0414
        private readonly int STATE_NONE = 0;
#pragma warning restore 0414
        private readonly int STATE_PLAYING = 1;
        private readonly int STATE_WAITING = 2;
        private readonly int STATE_STOPPING = 3;
        private readonly int STATE_STOPPED = 4;
        private readonly int BUFFER_SIZE = 16384;

        public DataPusher(ISourceDataLine sourceLine, AudioFormat format, byte[] audioData, int byteLength)
            : this(sourceLine, format, null, audioData, byteLength, false) {
        }

        public DataPusher(ISourceDataLine sourceLine, AudioFormat format,
                          byte[] audioData, int byteLength, bool daemon)
            : this(sourceLine, format, null, audioData, byteLength, daemon) {
        }

        public DataPusher(ISourceDataLine sourceLine, AudioInputStream ais)
            : this(sourceLine, ais.getFormat(), ais, null, 0, false) {
        }

        private DataPusher(ISourceDataLine source, AudioFormat format,
                           AudioInputStream ais, byte[] audioData,
                           int audioDataByteLength,
                           bool daemon) {
            this.source = source;
            this.format = format;
            this.ais = ais;
            this.audioDataByteLength = audioDataByteLength;
            if (audioData == null) {
                this.audioData = null;
            } else {
                this.audioData = new byte[audioData.Length];
                Array.Copy(audioData, this.audioData, audioData.Length);
            }
            //this.audioData = audioData == null ? null : Arrays.copyOf(audioData, audioData.Length);
            this.daemonThread = daemon;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void start() {
            start(false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool isPlaying() {
            return threadState == STATE_PLAYING;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void start(bool loop) {
            try {
                if (threadState == STATE_STOPPING) {
                    // wait that the thread has finished stopping
                    stop();
                }
                looping = loop;
                newPos = 0;
                wantedState = STATE_PLAYING;
                if (!source.isOpen()) {
                    source.open(format);
                }
                source.flush();
                source.start();
                if (pushThread == null) {
                    pushThread = JSSecurityManager.createThread(this.run,
                                           "DataPusher",   // name
                                           daemonThread,  // daemon
                                           ThreadPriority.Normal, //-1,    // priority
                                           true); // doStart
                }
                Monitor.PulseAll(this);
            } catch (Exception e) {
                if (Printer.err) printStackTrace(e);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void stop() {
            if (threadState == STATE_STOPPING
                || threadState == STATE_STOPPED
                || pushThread == null) {
                return;
            }

            wantedState = STATE_WAITING;
            if (source != null) {
                source.flush();
            }
            Monitor.PulseAll(this);
            int maxWaitCount = 50; // 5 seconds
            while ((maxWaitCount-- >= 0) && (threadState == STATE_PLAYING)) {
                try {
                    Monitor.Wait(this, 100);
                } catch (ThreadInterruptedException) { }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void close() {
            if (source != null) {
                source.close();
            }
        }

        /**
         * Write data to the source data line.
         */
        public void run() {
            byte[] buffer = null;
            bool useStream = (ais != null);
            if (useStream) {
                buffer = new byte[BUFFER_SIZE];
            } else {
                buffer = audioData;
            }
            while (wantedState != STATE_STOPPING) {
                //try {
                if (wantedState == STATE_WAITING) {
                    // wait for 5 seconds - maybe the clip is to be played again
                    try {
                        lock (this) {
                            threadState = STATE_WAITING;
                            wantedState = STATE_STOPPING;
                            Monitor.Wait(this, AUTO_CLOSE_TIME);
                        }
                    } catch (ThreadInterruptedException) { }
                    continue;
                }
                if (newPos >= 0) {
                    pos = newPos;
                    newPos = -1;
                }
                threadState = STATE_PLAYING;
                int toWrite = BUFFER_SIZE;
                if (useStream) {
                    try {
                        pos = 0; // always write from beginning of buffer
                        // don't use read(byte[]), because some streams
                        // may not override that method
                        toWrite = ais.Read(buffer, 0, buffer.Length);
                    } catch (IOException) {
                        // end of stream
                        toWrite = -1;
                    }
                } else {
                    if (toWrite > audioDataByteLength - pos) {
                        toWrite = audioDataByteLength - pos;
                    }
                    if (toWrite == 0) {
                        toWrite = -1; // end of "stream"
                    }
                }
                if (toWrite < 0) {
                    if (!useStream && looping) {
                        pos = 0;
                        continue;
                    }
                    wantedState = STATE_WAITING;
                    source.drain();
                    continue;
                }
                int bytesWritten = source.write(buffer, pos, toWrite);
                pos += bytesWritten;
            }
            threadState = STATE_STOPPING;
            source.flush();
            source.stop();
            source.flush();
            source.close();
            threadState = STATE_STOPPED;
            lock (this) {
                pushThread = null;
                Monitor.PulseAll(this);
            }
        }

        private void printStackTrace(Exception ex) {
            Printer.printStackTrace(ex);
        }

    } // class DataPusher
}
