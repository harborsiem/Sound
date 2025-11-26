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

//import javax.sound.midi.MidiDevice;
//import javax.sound.midi.spi.MidiDeviceProvider;

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {

/**
 * Super class for MIDI input or output device provider.
 *
 * @author Florian Bomers
 */
    public abstract class AbstractMidiDeviceProvider : MidiDeviceProvider {

        private static readonly bool enabled;

        /**
         * Create objects representing all MIDI output devices on the system.
         */
        static AbstractMidiDeviceProvider() {
            Platform.initialize();
            enabled = Platform.isMidiIOEnabled();

            // $$fb number of MIDI devices may change with time
            // also for memory's sake, do not initialize the arrays here
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void readDeviceInfos() {
            Info[] infos = getInfoCache();
            IMidiDevice[] devices = getDeviceCache();
            if (!enabled) {
                if (infos == null || infos.Length != 0) {
                    setInfoCache(new Info[0]);
                }
                if (devices == null || devices.Length != 0) {
                    setDeviceCache(new IMidiDevice[0]);
                }
                return;
            }

            int oldNumDevices = (infos == null) ? -1 : infos.Length;
            int newNumDevices = getNumDevices();
            if (oldNumDevices != newNumDevices) {

                // initialize the arrays
                Info[] newInfos = new Info[newNumDevices];
                IMidiDevice[] newDevices = new IMidiDevice[newNumDevices];

                for (int i = 0; i < newNumDevices; i++) {
                    Info newInfo = createInfo(i);

                    // in case that we are re-reading devices, try to find
                    // the previous one and reuse it
                    if (infos != null) {
                        for (int ii = 0; ii < infos.Length; ii++) {
                            Info info = infos[ii];
                            if (info != null && info.equalStrings(newInfo)) {
                                // new info matches the still existing info. Use old one
                                newInfos[i] = info;
                                info.setIndex(i);
                                infos[ii] = null; // prevent re-use
                                newDevices[i] = devices[ii];
                                devices[ii] = null;
                                break;
                            }
                        }
                    }
                    if (newInfos[i] == null) {
                        newInfos[i] = newInfo;
                    }
                }
                // the remaining MidiDevice.Info instances in the infos array
                // have become obsolete.
                if (infos != null) {
                    for (int i = 0; i < infos.Length; i++) {
                        if (infos[i] != null) {
                            // disable this device info
                            infos[i].setIndex(-1);
                        }
                        // what to do with the MidiDevice instances that are left
                        // in the devices array ?? Close them ?
                    }
                }
                // commit new list of infos.
                setInfoCache(newInfos);
                setDeviceCache(newDevices);
            }
        }

        public sealed override MidiDevice.Info[] getDeviceInfo() {
            readDeviceInfos();
            Info[] infos = getInfoCache();
            MidiDevice.Info[] localArray = new MidiDevice.Info[infos.Length];
            Array.Copy(infos, 0, localArray, 0, infos.Length);
            return localArray;
        }

        public sealed override IMidiDevice getDevice(MidiDevice.Info info) {
            if (info is Info) {
                readDeviceInfos();
                IMidiDevice[] devices = getDeviceCache();
                Info[] infos = getInfoCache();
                Info thisInfo = (Info)info;
                int index = thisInfo.getIndex();
                if (index >= 0 && index < devices.Length && infos[index] == info) {
                    if (devices[index] == null) {
                        devices[index] = createDevice(thisInfo);
                    }
                    if (devices[index] != null) {
                        return devices[index];
                    }
                }
            }
            throw MidiUtils.unsupportedDevice(info);
        }

        /**
         * Info class for MidiDevices.  Adds an index value for
         * making native references to a particular device.
         */
        public class Info : MidiDevice.Info {
            private int index;

            internal Info(String name, String vendor, String description, String version, int index)
                : base(name, vendor, description, version) {
                this.index = index;
            }

            internal bool equalStrings(Info info) {
                return (info != null
                     && getName().Equals(info.getName())
                     && getVendor().Equals(info.getVendor())
                     && getDescription().Equals(info.getDescription())
                     && getVersion().Equals(info.getVersion()));
            }

            internal int getIndex() {
                return index;
            }

            internal void setIndex(int index) {
                this.index = index;
            }

        } // class Info

        public abstract int getNumDevices();
        public abstract IMidiDevice[] getDeviceCache();
        public abstract void setDeviceCache(IMidiDevice[] devices);
        public abstract Info[] getInfoCache();
        public abstract void setInfoCache(Info[] infos);

        public abstract Info createInfo(int index);
        public abstract IMidiDevice createDevice(Info info);
    }
}




