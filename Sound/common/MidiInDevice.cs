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

//import javax.sound.midi.MidiUnavailableException;
//import javax.sound.midi.Transmitter;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using SystemX.Addon;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {
/**
 * MidiInDevice class representing functionality of MidiIn devices.
 *
 * @author David Rivas
 * @author Kara Kytle
 * @author Florian Bomers
 */
    internal sealed partial class MidiInDevice : AbstractMidiDevice, IRunnable {

        private volatile Thread midiInThread;

        internal MidiInDevice(AbstractMidiDeviceProvider.Info info)
            : base(info) {
        }

        // $$kk: 06.24.99: i have this both opening and starting the midi in device.
        // may want to separate these??
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected unsafe override void implOpen() {
            int index = ((MidiInDeviceProvider.MidiInDeviceInfo)getDeviceInfo()).getIndex();
            MidiDeviceHandlePtr idLocal;
            idLocal = nOpen(index); // can throw MidiUnavailableException
            id = (IntPtr)idLocal.Value;

            if (idLocal.IsNull) {
                throw new MidiUnavailableException("Unable to open native device");
            }

            // create / start a thread to get messages
            if (midiInThread == null) {
                midiInThread = JSSecurityManager.createThread(this.run,
                                   "Java Sound MidiInDevice Thread",   // name
                                   false,  // daemon //a@ not changed
                                   ThreadPriority.Normal, //-1,    // priority
                                   true); // doStart
            }

            nStart(idLocal); // can throw MidiUnavailableException
        }

        // $$kk: 06.24.99: i have this both stopping and closing the midi in device.
        // may want to separate these??
        [MethodImpl(MethodImplOptions.Synchronized)]
        protected override void implClose() {
            MidiDeviceHandlePtr oldId = new MidiDeviceHandlePtr(id);
            id = IntPtr.Zero;
            base.implClose();

            // close the device
            nStop(oldId);
            if (midiInThread != null) {
                try {
                    midiInThread.Join(1000);
                } catch (ThreadInterruptedException) {
                    // IGNORE EXCEPTION
                }
            }
            nClose(oldId);
        }

        public override long getMicrosecondPosition() {
            long timestamp = -1;
            if (isOpen()) {
                timestamp = nGetTimeStamp(new MidiDeviceHandlePtr(id));
            }
            return timestamp;
        }

        // OVERRIDES OF ABSTRACT MIDI DEVICE METHODS

        protected override bool hasTransmitters() {
            return true;
        }

        protected override ITransmitter createTransmitter() {
            return new MidiInTransmitter(this);
        }

        /**
          * An own class to distinguish the class name from
          * the transmitter of other devices.
          */
        private sealed class MidiInTransmitter : BasicTransmitter {
            internal MidiInTransmitter(MidiInDevice caller)
                : base(caller) {
            }
        }

        public void run() {
            // while the device is started, keep trying to get messages.
            // this thread returns from native code whenever stop() or close() is called
            try { //a@ changed
                while (id != IntPtr.Zero) {
                    // go into native code and retrieve messages
                    nGetMessages(new MidiDeviceHandlePtr(id));
                    if (id != IntPtr.Zero) {
                        Thread.Sleep(1);
                    }
                }
            } catch (ThreadInterruptedException) { }
            // let the thread exit
            midiInThread = null;
        }

        /**
         * Callback from native code when a short MIDI event is received from hardware.
         * @param packedMsg: status | data1 << 8 | data2 << 8
         * @param timeStamp time-stamp in microseconds
         */
        internal void callbackShortMessage(int packedMsg, long timeStamp) {
            if (packedMsg == 0 || id == IntPtr.Zero) {
                return;
            }

            /*if(Printer.verbose) {
              int status = packedMsg & 0xFF;
              int data1 = (packedMsg & 0xFF00)>>8;
              int data2 = (packedMsg & 0xFF0000)>>16;
              Printer.verbose(">> MidiInDevice callbackShortMessage: status: " + status + " data1: " + data1 + " data2: " + data2 + " timeStamp: " + timeStamp);
              }*/

            getTransmitterList().sendMessage(packedMsg, timeStamp);
        }

        internal void callbackLongMessage(byte[] data, long timeStamp) {
            if (id == IntPtr.Zero || data == null) {
                return;
            }
            getTransmitterList().sendMessage(data, timeStamp);
        }

#if NoNative
        //Object = MidiDeviceHandle

        private Object nOpen(int index) { return null; }
        private void nClose(Object id) { }
        private void nStart(Object id) { }
        private void nStop(Object id) { }
        private long nGetTimeStamp(Object id) { return 0; }

        // go into native code and get messages. May be blocking
        private void nGetMessages(Object id) { }
#endif
    }
}
