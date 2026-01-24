/*
 * Copyright (c) 2010, 2013, Oracle and/or its affiliates. All rights reserved.
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

//import javax.sound.midi.MidiDevice;
//import javax.sound.midi.MidiDeviceReceiver;
//import javax.sound.midi.MidiMessage;
//import javax.sound.midi.Receiver;

using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {

/**
 * Helper class which allows to convert {@code Receiver}
 * to {@code MidiDeviceReceiver}.
 *
 * @author Alex Menkov
 */
    public sealed class MidiDeviceReceiverEnvelope : IMidiDeviceReceiver {

        private readonly IMidiDevice device;
        private readonly IReceiver receiver;

        /**
         * Creates a new {@code MidiDeviceReceiverEnvelope} object which
         * envelops the specified {@code Receiver}
         * and is owned by the specified {@code MidiDevice}.
         *
         * @param device the owner {@code MidiDevice}
         * @param receiver the {@code Receiver} to be enveloped
         */
        public MidiDeviceReceiverEnvelope(IMidiDevice device, IReceiver receiver) {
            if (device == null) {
                throw new ArgumentNullException(nameof(device));
            }
            if (receiver == null) {
                throw new ArgumentNullException(nameof(receiver));
            }
            this.device = device;
            this.receiver = receiver;
        }

        // Receiver implementation
        public void close() {
            receiver.close();
        }

        public void Dispose() {
            close();
        }

        public void send(MidiMessage message, long timeStamp) {
            receiver.send(message, timeStamp);
        }

        // MidiDeviceReceiver implementation
        public IMidiDevice getMidiDevice() {
            return device;
        }

        /**
         * Obtains the receiver enveloped
         * by this {@code MidiDeviceReceiverEnvelope} object.
         *
         * @return the enveloped receiver
         */
        public IReceiver getReceiver() {
            return receiver;
        }
    }
}

