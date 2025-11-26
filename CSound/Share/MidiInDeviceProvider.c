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

//#define USE_ERROR
//#define USE_TRACE


//#include <jni.h>
#include "SoundDefs.h"

// Platform MIDI includes
#include "PlatformMidi.h"
#include "Utilities.h"
// for strcpy
#include <string.h>
//#include "MidiInDeviceProvider.h"

#define MAX_STRING_LENGTH 128


DllExport INT32 __stdcall MidiInDeviceProvider_nGetNumDevices() {

    INT32 numDevices = 0;

    TRACE0("MidiInDeviceProvider_nGetNumDevices.\n");

#if USE_PLATFORM_MIDI_IN == TRUE
    numDevices = MIDI_IN_GetNumDevices();
#endif

    TRACE1("MidiInDeviceProvider_nGetNumDevices returning %d.\n", numDevices);
    return (INT32) numDevices;
}


DllExport void __stdcall MidiInDeviceProvider_nGetName(INT32 index, char* dnName, INT32 length) {

    char name[MAX_STRING_LENGTH + 1];

    TRACE0("MidiInDeviceProvider_nGetName.\n");
    name[0] = 0;

#if USE_PLATFORM_MIDI_IN == TRUE
    MIDI_IN_GetDeviceName((INT32)index, name, (UINT32)MAX_STRING_LENGTH);
#endif

    if (name[0] == 0) {
    strcpy(name, "Unknown name");
    }
    strcpy(dnName, name);
    TRACE0("MidiInDeviceProvider_nGetName completed.\n");
    return;
}


DllExport void __stdcall MidiInDeviceProvider_nGetVendor(INT32 index, char* vendor, INT32 length) {

    char name[MAX_STRING_LENGTH + 1];

    TRACE0("MidiInDeviceProvider_nGetVendor.\n");
    name[0] = 0;

#if USE_PLATFORM_MIDI_IN == TRUE
    MIDI_IN_GetDeviceVendor((INT32)index, name, (UINT32)MAX_STRING_LENGTH);
#endif

    if (name[0] == 0) {
    strcpy(name, "Unknown vendor");
    }
    strcpy(vendor, name);
    TRACE0("MidiInDeviceProvider_nGetVendor completed.\n");
    return;
}


DllExport void __stdcall MidiInDeviceProvider_nGetDescription(INT32 index, char* description, INT32 length) {

    char name[MAX_STRING_LENGTH + 1];

    TRACE0("MidiInDeviceProvider_nGetDescription.\n");
    name[0] = 0;

#if USE_PLATFORM_MIDI_IN == TRUE
    MIDI_IN_GetDeviceDescription((INT32)index, name, (UINT32)MAX_STRING_LENGTH);
#endif

    if (name[0] == 0) {
    strcpy(name, "No details available");
    }
    strcpy(description, name);
    TRACE0("MidiInDeviceProvider_nGetDescription completed.\n");
    return;
}


DllExport void __stdcall MidiInDeviceProvider_nGetVersion(INT32 index, char* version, INT32 length) {

    char name[MAX_STRING_LENGTH + 1];

    TRACE0("MidiInDeviceProvider_nGetVersion.\n");
    name[0] = 0;

#if USE_PLATFORM_MIDI_IN == TRUE
    MIDI_IN_GetDeviceVersion((INT32)index, name, (UINT32)MAX_STRING_LENGTH);
#endif

    if (name[0] == 0) {
    strcpy(name, "Unknown version");
    }
    strcpy(version, name);
    TRACE0("MidiInDeviceProvider_nGetVersion completed.\n");
    return;
}
