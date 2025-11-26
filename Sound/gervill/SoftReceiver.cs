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

//import java.util.TreeMap;

//import javax.sound.midi.MidiDevice;
//import javax.sound.midi.MidiDeviceReceiver;
//import javax.sound.midi.MidiMessage;
//import javax.sound.midi.ShortMessage;

using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {

/**
 * Software synthesizer MIDI receiver class.
 *
 * @author Karl Helgason
 */
    public sealed class SoftReceiver : IMidiDeviceReceiver {

        internal bool open = true;
        private readonly Object control_mutex;
        private readonly SoftSynthesizer synth;
        internal IDictionary<Int64, Object> midimessages; //TreeMap<Int64, Object>
        internal SoftMainMixer mainmixer;

        public SoftReceiver(SoftSynthesizer synth) {
            this.control_mutex = synth.control_mutex;
            this.synth = synth;
            this.mainmixer = synth.getMainMixer();
            if (mainmixer != null)
                this.midimessages = mainmixer.midimessages;
        }

        public IMidiDevice getMidiDevice() {
            return synth;
        }    

        public void send(MidiMessage message, long timeStamp) {

            lock (control_mutex) {
                if (!open)
                    throw new InvalidOperationException("Receiver is not open");
            }

            if (timeStamp != -1) {
                lock (control_mutex) {
                    mainmixer.activity();
                    while (midimessages.ContainsKey(timeStamp))
                        timeStamp++;
                    if (message is ShortMessage
                            && (((ShortMessage)message).getChannel() > 0xF)) {
                        midimessages[timeStamp] =  message.Clone();
                    } else {
                        midimessages[timeStamp] = message.getMessage();
                    }
                }
            } else {
                mainmixer.processMessage(message);
            }
        }

        public void close() {
            lock (control_mutex) {
                open = false;
            }
            synth.removeReceiver(this);
        }

        public void Dispose() {
            close();
        }
    }
}