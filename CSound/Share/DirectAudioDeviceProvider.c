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

 //#define USE_TRACE
 //#define USE_ERROR

//#include <jni.h>
#include <jni_util.h>
#include "SoundDefs.h"
#include "DirectAudio.h"
#include "Utilities.h"
//#include "DirectAudioDeviceProvider.h"

//////////////////////////////////////////// DirectAudioDeviceProvider ////////////////////////////////////////////

int getDirectAudioDeviceDescription(int mixerIndex, DirectAudioDeviceDescription* desc) {
	desc->deviceID = 0;
	desc->maxSimulLines = 0;
	strcpy(desc->name, "Unknown Name");
	strcpy(desc->vendor, "Unknown Vendor");
	strcpy(desc->description, "Unknown Description");
	strcpy(desc->version, "Unknown Version");
#if USE_DAUDIO == TRUE
	DAUDIO_GetDirectAudioDeviceDescription(mixerIndex, desc);
#endif // USE_DAUDIO
	return TRUE;
}

DllExport INT32 __stdcall DirectAudioDeviceProvider_nGetNumDevices(void) {
	INT32 numDevices = 0;

	TRACE0("DirectAudioDeviceProvider_nGetNumDevices.\n");

#if USE_DAUDIO == TRUE
	numDevices = DAUDIO_GetDirectAudioDeviceCount();
#endif // USE_DAUDIO

	TRACE1("DirectAudioDeviceProvider_nGetNumDevices returning %d.\n", (int)numDevices);

	return (INT32)numDevices;
}

DllExport BOOL __stdcall DirectAudioDeviceProvider_nNewDirectAudioDeviceInfo(INT32 mixerIndex, DirectAudioDeviceDescription* desc) {

	BOOL result = FALSE;
	TRACE1("DirectAudioDeviceProvider_nNewDirectAudioDeviceInfo(%d).\n", mixerIndex);


	TRACE1("Get description for device %d\n", mixerIndex);

	if (getDirectAudioDeviceDescription(mixerIndex, desc)) {
		result = TRUE;
	}
	else {
		ERROR1("ERROR: getDirectAudioDeviceDescription(%d, desc) returned FALSE!\n", mixerIndex);
	}

	TRACE0("DirectAudioDeviceProvider_nNewDirectAudioDeviceInfo succeeded.\n");
	return result;
}

