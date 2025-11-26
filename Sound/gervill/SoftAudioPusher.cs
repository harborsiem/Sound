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

//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.SourceDataLine;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * This is a processor object that writes into SourceDataLine
 *
 * @author Karl Helgason
 */
    public sealed class SoftAudioPusher { //: IRunnable

        private volatile bool active;
        private ISourceDataLine sourceDataLine;
        private Thread audiothread;
        private readonly AudioInputStream ais;
        private readonly byte[] buffer;

        public SoftAudioPusher(ISourceDataLine sourceDataLine, AudioInputStream ais,
                int workbuffersizer) {
            this.ais = ais;
            this.buffer = new byte[workbuffersizer];
            this.sourceDataLine = sourceDataLine;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void start() {
            if (active)
                return;
            active = true;
            audiothread = new Thread(this.run);
            audiothread.Name = "AudioPusher";
            audiothread.IsBackground = true;
            audiothread.Priority = ThreadPriority.AboveNormal; // (Thread.MAX_PRIORITY);
            audiothread.Start();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void stop() {
            if (!active)
                return;
            active = false;
            try {
                audiothread.Join();
            }
            catch (ThreadInterruptedException) {
                //e.printStackTrace();
            }
        }

        public void run() {
            byte[] buffer = this.buffer;
            AudioInputStream ais = this.ais;
            ISourceDataLine sourceDataLine = this.sourceDataLine;

            try {
                while (active) {
                    // Read from audio source
                    int count = ais.Read(buffer, 0, buffer.Length);
                    if (count <= 0) break;
                    // Write byte buffer to source output
                    sourceDataLine.write(buffer, 0, count);
                }
            }
            catch (IOException) {
                active = false;
                //Printer.printStackTrace(e);
            }
        }
    }
}
