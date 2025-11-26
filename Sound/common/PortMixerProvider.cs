#undef NoNative
//#define NoNative
/*
 * Copyright (c) 2002, 2019, Oracle and/or its affiliates. All rights reserved.
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

//import javax.sound.sampled.Mixer;
//import javax.sound.sampled.spi.MixerProvider;

using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {
/**
 * Port provider.
 *
 * @author Florian Bomers
 */
    public sealed partial class PortMixerProvider : MixerProvider {

        /**
         * Set of info objects for all port input devices on the system.
         */
        private static PortMixerInfo[] infos;

        /**
         * Set of all port input devices on the system.
         */
        private static PortMixer[] devices;

        static PortMixerProvider() {
            // initialize
            Platform.initialize();
        }

        /**
         * Required public no-arg constructor.
         */
        public PortMixerProvider() {
            lock (typeof(PortMixerProvider)) {
                if (Platform.isPortsEnabled()) {
                    init();
                } else {
                    infos = new PortMixerInfo[0];
                    devices = new PortMixer[0];
                }
            }
        }

        private static void init() {
            // get the number of input devices
            int numDevices = nGetNumDevices();

            if (infos == null || infos.Length != numDevices) {
                // initialize the arrays
                infos = new PortMixerInfo[numDevices];
                devices = new PortMixer[numDevices];

                // fill in the info objects now.
                // we'll fill in the device objects as they're requested.
                for (int i = 0; i < infos.Length; i++) {
                    infos[i] = nNewPortMixerInfo(i);
                }
            }
        }

        public override Mixer.Info[] getMixerInfo() {
            lock (typeof(PortMixerProvider)) {
                Mixer.Info[] localArray = new Mixer.Info[infos.Length];
                Array.Copy(infos, 0, localArray, 0, infos.Length);
                return localArray;
            }
        }

        public override IMixer getMixer(Mixer.Info info) {
            lock (typeof(PortMixerProvider)) {
                for (int i = 0; i < infos.Length; i++) {
                    if (infos[i].Equals(info)) {
                        return getDevice(infos[i]);
                    }
                }
            }
            throw new ArgumentException("Mixer " + info.ToString()
                + " not supported by this provider.");
        }

        private static IMixer getDevice(PortMixerInfo info) {
            int index = info.getIndex();
            if (devices[index] == null) {
                devices[index] = new PortMixer(info);
            }
            return devices[index];
        }

        /**
         * Info class for PortMixers.  Adds an index value for
         * making native references to a particular device.
         * This constructor is called from native.
         */
        internal sealed class PortMixerInfo : Mixer.Info {
            private readonly int index;

            internal PortMixerInfo(int index, String name, String vendor, String description, String version)
                : base("Port " + name, vendor, description, version) {
                this.index = index;
            }

            internal int getIndex() {
                return index;
            }
        } // class PortMixerInfo

#if NoNative
        private static int nGetNumDevices() { return 0; }

        private static PortMixerInfo nNewPortMixerInfo(int mixerIndex) { return null; }
#endif
    }
}
