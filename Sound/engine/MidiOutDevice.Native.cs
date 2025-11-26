/*
 * Copyright (c) 2002, 2007, Oracle and/or its affiliates. All rights reserved.
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

/*****************************************************************************/
/*
**      Native functions for interfacing Java with the native implementation
**      of PlatformMidi.h's functions.
*/
/*****************************************************************************/

using System;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32.SafeHandles;
using SystemX.Sound.Midi;
using Windows.Win32.Foundation;

namespace SystemX.Media.Sound {

    partial class MidiOutDevice {

        private unsafe MidiDeviceHandlePtr nOpen(int index) {
            int error;
            byte* bytes = stackalloc byte[128];
            PCSTR msg = new PCSTR(bytes);
            MidiDeviceHandlePtr id = NativeMethods.MidiOutDevice_nOpen(index, out error, msg);
            if (id.IsNull) {
                throw new MidiUnavailableException(msg.ToString());
            }
            return id;
        }

        private void nClose(MidiDeviceHandlePtr id) {
            NativeMethods.MidiOutDevice_nClose(id);
        }

        private void nSendShortMessage(MidiDeviceHandlePtr id, int packedMsg, long timeStamp) {
            NativeMethods.MidiOutDevice_nSendShortMessage(id, packedMsg, timeStamp);
        }

        private unsafe void nSendLongMessage(MidiDeviceHandlePtr id, byte[] data, int size, long timeStamp) {
            fixed (byte* dataLocal = data)
                NativeMethods.MidiOutDevice_nSendLongMessage(id, dataLocal, size, timeStamp);
        }

        private long nGetTimeStamp(MidiDeviceHandlePtr id) {
            return NativeMethods.MidiOutDevice_nGetTimeStamp(id);
        }

        private class NativeMethods {
            private const String CSound = "CSound.dll";

            [DllImport(CSound, CharSet = CharSet.Ansi, BestFitMapping = false)]
            public static extern MidiDeviceHandlePtr MidiOutDevice_nOpen(int index,
                out int error,
                PCSTR msg);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void MidiOutDevice_nClose(MidiDeviceHandlePtr id);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void MidiOutDevice_nSendShortMessage(MidiDeviceHandlePtr id, int packedMsg, long timeStamp);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public unsafe static extern void MidiOutDevice_nSendLongMessage(MidiDeviceHandlePtr id, byte* data, int size, long timeStamp);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern long MidiOutDevice_nGetTimeStamp(MidiDeviceHandlePtr id);
        }
    }
}
