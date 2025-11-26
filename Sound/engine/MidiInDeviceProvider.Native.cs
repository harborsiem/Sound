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
using System.Runtime.InteropServices;
using System.Security;
using Windows.Win32.Foundation;

namespace SystemX.Media.Sound {
    partial class MidiInDeviceProvider {
        private const int MAX_STRING_LENGTH = 128;

        private static int nGetNumDevices() {
            return NativeMethods.MidiInDeviceProvider_nGetNumDevices();
        }

        private static unsafe String nGetName(int index) {
            byte* bytes = stackalloc byte[MAX_STRING_LENGTH + 1];
            PUTF8STR utf8Str = new PUTF8STR(bytes);
            NativeMethods.MidiInDeviceProvider_nGetName(index, utf8Str, MAX_STRING_LENGTH);
            return utf8Str.ToString();
        }

        private static unsafe String nGetVendor(int index) {
            byte* bytes = stackalloc byte[MAX_STRING_LENGTH + 1];
            PUTF8STR utf8Str = new PUTF8STR(bytes);
            NativeMethods.MidiInDeviceProvider_nGetVendor(index, utf8Str, MAX_STRING_LENGTH);
            return utf8Str.ToString();
        }

        private static unsafe String nGetDescription(int index) {
            byte* bytes = stackalloc byte[MAX_STRING_LENGTH + 1];
            PUTF8STR utf8Str = new PUTF8STR(bytes);
            NativeMethods.MidiInDeviceProvider_nGetDescription(index, utf8Str, MAX_STRING_LENGTH);
            return utf8Str.ToString();
        }

        private static unsafe String nGetVersion(int index) {
            byte* bytes = stackalloc byte[MAX_STRING_LENGTH + 1];
            PUTF8STR utf8Str = new PUTF8STR(bytes);
            NativeMethods.MidiInDeviceProvider_nGetVersion(index, utf8Str, MAX_STRING_LENGTH);
            return utf8Str.ToString();
        }

        private class NativeMethods {
            private const String CSound = "CSound.dll";

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern int MidiInDeviceProvider_nGetNumDevices();

            [DllImport(CSound, CharSet = CharSet.Ansi, BestFitMapping = false)]
            public static extern void MidiInDeviceProvider_nGetName(
                int index,
                PUTF8STR name, int length);

            [DllImport(CSound, CharSet = CharSet.Ansi, BestFitMapping = false)]
            public static extern void MidiInDeviceProvider_nGetVendor(
                int index,
                PUTF8STR vendor, int length);

            [DllImport(CSound, CharSet = CharSet.Ansi, BestFitMapping = false)]
            public static extern void MidiInDeviceProvider_nGetDescription(
                int index,
                PUTF8STR description, int length);

            [DllImport(CSound, CharSet = CharSet.Ansi, BestFitMapping = false)]
            public static extern void MidiInDeviceProvider_nGetVersion(
                int index,
                PUTF8STR version, int length);
        }
    }
}
