/*
 * Copyright (c) 2002, 2014, Oracle and/or its affiliates. All rights reserved.
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
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Diagnostics;
using Windows.Win32.Foundation;

namespace SystemX.Media.Sound {
    partial class DirectAudioDeviceProvider {
        private const int DAUDIO_STRING_LENGTH = 200;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        //[BestFitMapping(false, ThrowOnUnmappableChar = true)]
        private unsafe struct DirectAudioDeviceDescription {
            // optional deviceID (complementary to deviceIndex)
            public int deviceID;
            public int maxSimulLines;
            public fixed byte name[DAUDIO_STRING_LENGTH + 1];
            public fixed byte vendor[DAUDIO_STRING_LENGTH + 1];
            public fixed byte description[DAUDIO_STRING_LENGTH + 1];
            public fixed byte version[DAUDIO_STRING_LENGTH + 1];
        }

        // NATIVE METHODS
        private static int nGetNumDevices() {
            return NativeMethods.DirectAudioDeviceProvider_nGetNumDevices();
        }

        // index: [0..nGetNumDevices()-1]
        private unsafe static DirectAudioDeviceInfo nNewDirectAudioDeviceInfo(int deviceIndex) {
            DirectAudioDeviceDescription desc; // = new DirectAudioDeviceDescription();
            DirectAudioDeviceInfo info = null;
            string name;
            string vendor;
            string description;
            string version;

            if (NativeMethods.DirectAudioDeviceProvider_nNewDirectAudioDeviceInfo(deviceIndex, out desc)) {
                name = new PUTF8STR(desc.name).ToString();
                if (name == null) return info;
                vendor = new PUTF8STR(desc.vendor).ToString();
                if (vendor == null) return info;
                description = new PUTF8STR(desc.description).ToString();
                if (description == null) return info;
                version = new PUTF8STR(desc.version).ToString();
                if (version == null) return info;
                info = new DirectAudioDeviceInfo(deviceIndex, desc.deviceID, desc.maxSimulLines,
                    name, vendor, description, version);
            }
            return info;
        }

        private class NativeMethods {
            private const String CSound = "CSound.dll";

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern int DirectAudioDeviceProvider_nGetNumDevices();

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern BOOL DirectAudioDeviceProvider_nNewDirectAudioDeviceInfo(
                int deviceIndex,
                [Out] out DirectAudioDeviceDescription desc);
        }
    }
}
