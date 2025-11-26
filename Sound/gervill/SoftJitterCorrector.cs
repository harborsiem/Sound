/*
 * Copyright (c) 2007, 2015, Oracle and/or its affiliates. All rights reserved.
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

//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioInputStream;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * A jitter corrector to be used with SoftAudioPusher.
 *
 * @author Karl Helgason
 */
    public sealed class SoftJitterCorrector : AudioInputStream {

        private class JitterStream : InputStream {

            internal static int MAX_BUFFER_SIZE = 1048576;
            internal bool active = true;
            internal Thread thread;
            internal AudioInputStream stream;
            // Cyclic buffer
            internal int writepos = 0;
            internal int readpos = 0;
            internal byte[][] buffers;
            internal readonly Object buffers_mutex = new Object();

            // Adapative Drift Statistics
            internal int w_count = 1000;
            internal int w_min_tol = 2;
            internal int w_max_tol = 10;
            internal int w = 0;
            internal int w_min = -1;
            // Current read buffer
            internal int bbuffer_pos = 0;
            internal int bbuffer_max = 0;
            internal byte[] bbuffer = null;

            public override long Position {
                get { return bbuffer_pos; }
            }

            public override long Length {
                get { return bbuffer.Length; }
            }

            public byte[] nextReadBuffer() {
                lock (buffers_mutex) {
                    if (writepos > readpos) {
                        int w_m = writepos - readpos;
                        if (w_m < w_min)
                            w_min = w_m;

                        int buffpos = readpos;
                        readpos++;
                        return buffers[buffpos % buffers.Length];
                    }
                    w_min = -1;
                    w = w_count - 1;
                }
                while (true) {
                    try {
                        Thread.Sleep(1);
                    }
                    catch (ThreadInterruptedException) {
                        //Printer.printStackTrace(e);
                        return null;
                    }
                    lock (buffers_mutex) {
                        if (writepos > readpos) {
                            w = 0;
                            w_min = -1;
                            w = w_count - 1;
                            int buffpos = readpos;
                            readpos++;
                            return buffers[buffpos % buffers.Length];
                        }
                    }
                }
            }

            public byte[] nextWriteBuffer() {
                lock (buffers_mutex) {
                    return buffers[writepos % buffers.Length];
                }
            }

            public void commit() {
                lock (buffers_mutex) {
                    writepos++;
                    if ((writepos - readpos) > buffers.Length) {
                        int bufflen = buffers[0].Length;
                        int newsize = (writepos - readpos) + 10;
                        newsize = Math.Max(buffers.Length * 2, newsize);
                        buffers = new byte[newsize][];
                        for (int i = 0; i < buffers.Length; i++) {
                            buffers[i] = new byte[bufflen];
                        }
                    }
                }
            }

            internal JitterStream(AudioInputStream s, int buffersize,
                    int smallbuffersize) {
                this.w_count = 10 * (buffersize / smallbuffersize);
                if (w_count < 100)
                    w_count = 100;
                this.buffers
                        = new byte[(buffersize / smallbuffersize) + 10][];
                for (int i = 0; i < this.buffers.Length; i++) {
                    this.buffers[i] = new byte[smallbuffersize];
                }
                this.bbuffer_max = MAX_BUFFER_SIZE / smallbuffersize;
                this.stream = s;

                RunnableImpl runnable = new RunnableImpl(this);

                thread = new Thread(runnable.run);
                thread.Name = "JitterCorrector";
                thread.IsBackground = true;
                thread.Priority = ThreadPriority.AboveNormal; //(Thread.MAX_PRIORITY);
                thread.Start();
            }

            private class RunnableImpl {
                JitterStream caller;

                public RunnableImpl(JitterStream caller) {
                    this.caller = caller;
                }

                public void run() {
                    AudioFormat format = caller.stream.getFormat();
                    int bufflen = caller.buffers[0].Length;
                    int frames = bufflen / format.getFrameSize();
                    long nanos = (long)(frames * 1000000000.0
                                            / format.getSampleRate());
                    long now = (long)Environment.TickCount * 1000000L; //System.nanoTime();
                    long next = now + nanos;
                    int correction = 0;
                    while (true) {
                        lock (caller) {
                            if (!caller.active)
                                break;
                        }
                        int curbuffsize;
                        lock (caller.buffers) {
                            curbuffsize = caller.writepos - caller.readpos;
                            if (correction == 0) {
                                caller.w++;
                                if (caller.w_min != Int32.MaxValue) {
                                    if (caller.w == caller.w_count) {
                                        correction = 0;
                                        if (caller.w_min < caller.w_min_tol) {
                                            correction = (caller.w_min_tol + caller.w_max_tol)
                                                            / 2 - caller.w_min;
                                        }
                                        if (caller.w_min > caller.w_max_tol) {
                                            correction = (caller.w_min_tol + caller.w_max_tol)
                                                            / 2 - caller.w_min;
                                        }
                                        caller.w = 0;
                                        caller.w_min = Int32.MaxValue;
                                    }
                                }
                            }
                        }
                        while (curbuffsize > caller.bbuffer_max) {
                            lock (caller.buffers) {
                                curbuffsize = caller.writepos - caller.readpos;
                            }
                            lock (caller) {
                                if (!caller.active)
                                    break;
                            }
                            try {
                                Thread.Sleep(1);
                            }
                            catch (ThreadInterruptedException) {
                                //Printer.printStackTrace(e);
                            }
                        }

                        if (correction < 0)
                            correction++;
                        else {
                            byte[] buff = caller.nextWriteBuffer();
                            try {
                                int n = 0;
                                while (n != buff.Length) {
                                    int s = caller.stream.Read(buff, n, buff.Length
                                            - n);
                                    if (s < 0)
                                        throw new EndOfStreamException();
                                    if (s == 0)
                                        //Thread.Sleep(0);
                                        Thread.Yield();
                                    n += s;
                                }
                            }
                            catch (IOException) {
                                //Printer.printStackTrace(e1);
                            }
                            caller.commit();
                        }

                        if (correction > 0) {
                            correction--;
                            next = (long)Environment.TickCount * 1000000L + nanos;
                            continue;
                        }
                        long wait = next - (long)Environment.TickCount * 1000000L;
                        if (wait > 0) {
                            try {
                                Thread.Sleep((int)(wait / 1000000L));
                            }
                            catch (ThreadInterruptedException) {
                                //Printer.printStackTrace(e);
                            }
                        }
                        next += nanos;
                    }
                }
            }

            public override void Close() {
                lock (this) {
                    active = false;
                }
                try {
                    thread.Join();
                }
                catch (ThreadInterruptedException) {
                    //Printer.printStackTrace(e);
                }
                stream.Close();
            }

            public override int ReadByte() {
                byte[] b = new byte[1];
                if (Read(b, 0, b.Length) <= 0)
                    return -1;
                return b[0] & 0xFF;
            }

            public void fillBuffer() {
                bbuffer = nextReadBuffer();
                bbuffer_pos = 0;
            }

            public override int Read(byte[] b, int off, int len) {
                if (bbuffer == null)
                    fillBuffer();
                int bbuffer_len = bbuffer.Length;
                int offlen = off + len;
                while (off < offlen) {
                    if (available() == 0)
                        fillBuffer();
                    else {
                        byte[] bbuffer0 = this.bbuffer;
                        int bbuffer_pos = this.bbuffer_pos;
                        while (off < offlen && bbuffer_pos < bbuffer_len)
                            b[off++] = bbuffer0[bbuffer_pos++];
                        this.bbuffer_pos = bbuffer_pos;
                    }
                }
                return len;
            }

            public override int available() {
                return bbuffer.Length - bbuffer_pos;
            }
        }

        public SoftJitterCorrector(AudioInputStream stream, int buffersize,
                int smallbuffersize)
            : base(new JitterStream(stream, buffersize, smallbuffersize),
                    stream.getFormat(), stream.getFrameLength()) {
        }
    }
}
