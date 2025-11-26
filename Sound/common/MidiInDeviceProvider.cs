#undef NoNative
//#define NoNative
/*
 * Copyright (c) 1999, 2019, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {
/**
 * MIDI input device provider.
 *
 * @author Kara Kytle
 * @author Florian Bomers
 */
    public sealed partial class MidiInDeviceProvider : AbstractMidiDeviceProvider {

        /** Cache of info objects for all MIDI output devices on the system. */
        private static Info[] infos = null;

        /** Cache of open MIDI input devices on the system. */
        private static IMidiDevice[] devices = null;

        private static readonly bool enabled;

        static MidiInDeviceProvider() {
            // initialize
            Platform.initialize();
            enabled = Platform.isMidiIOEnabled();
        }

        /**
         * Required public no-arg constructor.
         */
        public MidiInDeviceProvider() {
        }

        // implementation of abstract methods in AbstractMidiDeviceProvider

        public override AbstractMidiDeviceProvider.Info createInfo(int index) {
            if (!enabled) {
                return null;
            }
            return new MidiInDeviceInfo(index, typeof(MidiInDeviceProvider));
        }

        public override IMidiDevice createDevice(AbstractMidiDeviceProvider.Info info) {
            if (enabled && (info is MidiInDeviceInfo)) {
                return new MidiInDevice(info);
            }
            return null;
        }

        public override int getNumDevices() {
            if (!enabled) {
                return 0;
            }
            int numDevices = nGetNumDevices();
            return numDevices;
        }

        public override IMidiDevice[] getDeviceCache() { return devices; }
        public override void setDeviceCache(IMidiDevice[] devices) { MidiInDeviceProvider.devices = devices; }
        public override Info[] getInfoCache() { return infos; }
        public override void setInfoCache(Info[] infos) { MidiInDeviceProvider.infos = infos; }

        /**
         * Info class for MidiInDevices.  Adds the
         * provider's Class to keep the provider class from being
         * unloaded.  Otherwise, at least on JDK1.1.7 and 1.1.8,
         * the provider class can be unloaded.  Then, then the provider
         * is next invoked, the static block is executed again and a new
         * instance of the device object is created.  Even though the
         * previous instance may still exist and be open / in use / etc.,
         * the new instance will not reflect that state...
         */
        internal sealed class MidiInDeviceInfo : AbstractMidiDeviceProvider.Info {
            private readonly Type providerClass;

            internal MidiInDeviceInfo(int index, Type providerClass)
                : base(nGetName(index), nGetVendor(index), nGetDescription(index), nGetVersion(index), index) {
                this.providerClass = providerClass;
            }

        } // class MidiInDeviceInfo

#if NoNative
        // NATIVE METHODS

        private static int nGetNumDevices() { return 0; }
        private static String nGetName(int index) { return String.Empty; }
        private static String nGetVendor(int index) { return String.Empty; }
        private static String nGetDescription(int index) { return String.Empty; }
        private static String nGetVersion(int index) { return String.Empty; }
#endif
    }
}
