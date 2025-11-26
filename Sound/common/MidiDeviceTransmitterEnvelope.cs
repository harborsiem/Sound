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

//import javax.sound.midi.*;

using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {

/**
 * Helper class which allows to convert {@code Transmitter}
 * to {@code MidiDeviceTransmitter}.
 *
 * @author Alex Menkov
 */
    public sealed class MidiDeviceTransmitterEnvelope : IMidiDeviceTransmitter {

        private readonly IMidiDevice device;
        private readonly ITransmitter transmitter;

        /**
         * Creates a new {@code MidiDeviceTransmitterEnvelope} object which
         * envelops the specified {@code Transmitter}
         * and is owned by the specified {@code MidiDevice}.
         *
         * @param device the owner {@code MidiDevice}
         * @param transmitter the {@code Transmitter} to be enveloped
         */
        public MidiDeviceTransmitterEnvelope(IMidiDevice device, ITransmitter transmitter) {
            if (device == null) {
                throw new ArgumentNullException(nameof(device));
            }
            if (transmitter == null) {
                throw new ArgumentNullException(nameof(transmitter));
            }
            this.device = device;
            this.transmitter = transmitter;
        }

        // Transmitter implementation
        public void setReceiver(IReceiver receiver) {
            transmitter.setReceiver(receiver);
        }

        public IReceiver getReceiver() {
            return transmitter.getReceiver();
        }

        public void close() {
            transmitter.close();
        }

        public void Dispose() {
            close();
        }


        // MidiDeviceReceiver implementation
        public IMidiDevice getMidiDevice() {
            return device;
        }

        /**
         * Obtains the transmitter enveloped
         * by this {@code MidiDeviceTransmitterEnvelope} object.
         *
         * @return the enveloped transmitter
         */
        public ITransmitter getTransmitter() {
            return transmitter;
        }
    }
}
