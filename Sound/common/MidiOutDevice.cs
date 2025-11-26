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

//import javax.sound.midi.*;

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {
/**
 * MidiOutDevice class representing functionality of MidiOut devices.
 *
 * @author David Rivas
 * @author Kara Kytle
 * @author Florian Bomers
 */
    internal sealed partial class MidiOutDevice : AbstractMidiDevice {

        internal MidiOutDevice(AbstractMidiDeviceProvider.Info info)
            : base(info) {
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected unsafe override void implOpen() {
            int index = ((AbstractMidiDeviceProvider.Info)getDeviceInfo()).getIndex();
            MidiDeviceHandlePtr idLocal;
            idLocal = nOpen(index); // can throw MidiUnavailableException
            id = (IntPtr)idLocal.Value;
            if (idLocal.IsNull) {
                throw new MidiUnavailableException("Unable to open native device");
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected override void implClose() {
            // prevent further action
            MidiDeviceHandlePtr oldId = new MidiDeviceHandlePtr(id);
            id = IntPtr.Zero;
            base.implClose();

            // close the device
            nClose(oldId);
        }

        public override long getMicrosecondPosition() {
            long timestamp = -1;
            if (isOpen()) {
                timestamp = nGetTimeStamp(new MidiDeviceHandlePtr(id));
            }
            return timestamp;
        }

        /** Returns if this device supports Receivers.
        This implementation always returns true.
        @return true, if the device supports Receivers, false otherwise.
        */
        protected override bool hasReceivers() {
            return true;
        }

        protected override IReceiver createReceiver() {
            return new MidiOutReceiver(this);
        }

        internal sealed class MidiOutReceiver : AbstractReceiver {

            private MidiOutDevice caller;

            internal MidiOutReceiver(MidiOutDevice caller)
                : base(caller) {
                this.caller = caller;
            }

            internal override void implSend(MidiMessage message, long timeStamp) {
                int length = message.getLength();
                int status = message.getStatus();
                if (length <= 3 && status != 0xF0 && status != 0xF7) {
                    int packedMsg;
                    if (message is ShortMessage) {
                        if (message is FastShortMessage) {
                            packedMsg = ((FastShortMessage)message).getPackedMsg();
                        } else {
                            ShortMessage msg = (ShortMessage)message;
                            packedMsg = (status & 0xFF)
                                | ((msg.getData1() & 0xFF) << 8)
                                | ((msg.getData2() & 0xFF) << 16);
                        }
                    } else {
                        packedMsg = 0;
                        byte[] data = message.getMessage();
                        if (length > 0) {
                            packedMsg = data[0] & 0xFF;
                            if (length > 1) {
                                /* We handle meta messages here. The message
                                   system reset (FF) doesn't get until here,
                                   because it's length is only 1. So if we see
                                   a status byte of FF, it's sure that we
                                   have a Meta message. */
                                if (status == 0xFF) {
                                    return;
                                }
                                packedMsg |= (data[1] & 0xFF) << 8;
                                if (length > 2) {
                                    packedMsg |= (data[2] & 0xFF) << 16;
                                }
                            }
                        }
                    }
                    caller.nSendShortMessage(new MidiDeviceHandlePtr(caller.id), packedMsg, timeStamp);
                } else {
                    byte[] data;
                    if (message is FastSysexMessage) {
                        data = ((FastSysexMessage)message).getReadOnlyMessage();
                    } else {
                        data = message.getMessage();
                    }
                    int dataLength = Math.Min(length, data.Length);
                    if (dataLength > 0) {
                        caller.nSendLongMessage(new MidiDeviceHandlePtr(caller.id), data, dataLength, timeStamp);
                    }
                }
            }

            /** shortcut for the Sun implementation */
            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void sendPackedMidiMessage(int packedMsg, long timeStamp) {
                MidiDeviceHandlePtr id = new MidiDeviceHandlePtr(caller.id);
                if (isOpen() && !id.IsNull) {
                    caller.nSendShortMessage(id, packedMsg, timeStamp);
                }
            }
        } // class MidiOutReceiver

#if NoNative
        //Object = MidiDeviceHandle

        private Object nOpen(int index) { return null; }
        private void nClose(Object id) { }
        private void nSendShortMessage(Object id, int packedMsg, long timeStamp) { }
        private void nSendLongMessage(Object id, byte[] data, int size, long timeStamp) { }
        private long nGetTimeStamp(Object id) { return 0; }
#endif
    } // class MidiOutDevice
}
