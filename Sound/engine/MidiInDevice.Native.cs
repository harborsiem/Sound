/*
 * Copyright (c) 1999, 2007, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;
using System.Security;
using SystemX.Sound.Midi;
using Windows.Win32.Foundation;

namespace SystemX.Media.Sound {
    partial class MidiInDevice {
        delegate void ShortMsgPtr(int packedMsg, long timeStamp);
        delegate void LongMsgPtr(IntPtr data, int dataLength, long timeStamp);

        void callbackLongMsgFromC(IntPtr data, int dataLength, long timeStamp) {
            byte[] buffer = new byte[dataLength];
            Marshal.Copy(data, buffer, 0, dataLength);
            callbackLongMessage(buffer, timeStamp);
        }

        private unsafe MidiDeviceHandlePtr nOpen(int index) {
            int error;
            byte* bytes = stackalloc byte[128];
            PCSTR msg = new PCSTR(bytes);
            MidiDeviceHandlePtr id = NativeMethods.MidiInDevice_nOpen(index, out error, msg);
            if (id.IsNull) {
                throw new MidiUnavailableException(msg.ToString());
            }
            return id;
        }

        private void nClose(MidiDeviceHandlePtr id) {
            NativeMethods.MidiInDevice_nClose(id);
        }

        private unsafe void nStart(MidiDeviceHandlePtr id) {
            byte* bytes = stackalloc byte[128];
            PCSTR msg = new PCSTR(bytes);
            int err = NativeMethods.MidiInDevice_nStart(id, msg);
            if (err != 0) {
                throw new MidiUnavailableException(msg.ToString());
            }
        }

        private void nStop(MidiDeviceHandlePtr id) {
            NativeMethods.MidiInDevice_nStop(id);
        }

        private long nGetTimeStamp(MidiDeviceHandlePtr id) {
            return NativeMethods.MidiInDevice_nGetTimeStamp(id);
        }

        // go into native code and get messages. May be blocking
        private void nGetMessages(MidiDeviceHandlePtr id) {
            NativeMethods.MidiInDevice_nGetMessages(id, callbackShortMessage, callbackLongMsgFromC);
        }

        private class NativeMethods {
            private const String CSound = "CSound.dll";

            [DllImport(CSound, CharSet = CharSet.Ansi, BestFitMapping = false)]
            public static extern MidiDeviceHandlePtr MidiInDevice_nOpen(
                int index,
                out int error,
                PCSTR msg);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void MidiInDevice_nClose(MidiDeviceHandlePtr id);

            [DllImport(CSound, CharSet = CharSet.Ansi, BestFitMapping = false)]
            public static extern int MidiInDevice_nStart(MidiDeviceHandlePtr id,
                PCSTR msg);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void MidiInDevice_nStop(MidiDeviceHandlePtr id);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern long MidiInDevice_nGetTimeStamp(MidiDeviceHandlePtr id);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void MidiInDevice_nGetMessages(
                MidiDeviceHandlePtr id,
                [MarshalAs(UnmanagedType.FunctionPtr)]
                ShortMsgPtr shortMsg,
                [MarshalAs(UnmanagedType.FunctionPtr)]
                LongMsgPtr longMsg);
        }
    }
}

