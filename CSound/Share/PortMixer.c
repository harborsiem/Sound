/*
 * Copyright (c) 2002, 2024, Oracle and/or its affiliates. All rights reserved.
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
#define USE_ERROR

//#include <jni.h>
#include <jni_util.h>
#include "SoundDefs.h"
#include "Ports.h"
#include "Utilities.h"
//#include "PortMixer.h"

//////////////////////////////////////////// PortMixer ////////////////////////////////////////////

DllExport INT_PTR __stdcall PortMixer_nOpen(INT32 mixerIndex) {

    INT_PTR ret = 0;
#if USE_PORTS == TRUE
    ret = (INT_PTR) PORT_Open(mixerIndex);
#endif
    return ret;
}

DllExport void __stdcall PortMixer_nClose(INT_PTR id) {

#if USE_PORTS == TRUE
    if (id != 0) {
    PORT_Close((void*) (INT_PTR) id);
    }
#endif
}

DllExport INT32 __stdcall PortMixer_nGetPortCount(INT_PTR id) {

    INT32 ret = 0;
#if USE_PORTS == TRUE
    if (id != 0) {
    ret = PORT_GetPortCount((void*) (INT_PTR) id);
    }
#endif
    return ret;
}


DllExport INT32 __stdcall PortMixer_nGetPortType(INT_PTR id, INT32 portIndex) {

    INT32 ret = 0;
    TRACE1("PortMixer_nGetPortType(%d).\n", portIndex);

#if USE_PORTS == TRUE
    if (id != 0) {
    ret = (INT32) PORT_GetPortType((void*) (INT_PTR) id, portIndex);
    }
#endif

    TRACE1("PortMixerProvider_nGetPortType returning %d.\n", ret);
    return ret;
}

DllExport void __stdcall PortMixer_nGetPortName(INT_PTR id, INT32 portIndex, char* dnString) {

    char str[PORT_STRING_LENGTH];
    //char* jString = NULL;
    TRACE1("PortMixer_nGetPortName(%d).\n", portIndex);

    str[0] = 0;
#if USE_PORTS == TRUE
    if (id != 0) {
    PORT_GetPortName((void*) (INT_PTR) id, portIndex, str, PORT_STRING_LENGTH);
    }
#endif
    //jString = &str[0];
    strcpy(dnString, &str[0]);

    TRACE1("PortMixerProvider_nGetName returning \"%s\".\n", str);
    return;
}

DllExport void __stdcall PortMixer_nControlSetIntValue(UINT_PTR controlID, INT32 value) {
#if USE_PORTS == TRUE
    if (controlID != 0) {
    PORT_SetIntValue((void*) (UINT_PTR) controlID, (INT32) value);
    }
#endif
}

DllExport INT32 __stdcall PortMixer_nControlGetIntValue(UINT_PTR controlID) {
    INT32 ret = 0;
#if USE_PORTS == TRUE
    if (controlID != 0) {
    ret = PORT_GetIntValue((void*) (UINT_PTR) controlID);
    }
#endif
    return  ret;
}

DllExport void __stdcall PortMixer_nControlSetFloatValue(UINT_PTR controlID, float value) {
#if USE_PORTS == TRUE
    if (controlID != 0) {
    PORT_SetFloatValue((void*) (UINT_PTR) controlID, (float) value);
    }
#endif
}

DllExport float __stdcall PortMixer_nControlGetFloatValue(UINT_PTR controlID) {
    float ret = 0;
#if USE_PORTS == TRUE
    if (controlID != 0) {
    ret = PORT_GetFloatValue((void*) (UINT_PTR) controlID);
    }
#endif
    return (float) ret;
}


// contains all the needed references so that the platform dependent code can call JNI wrapper functions
typedef struct tag_ControlCreatorJNI {
    // this member is seen by the platform dependent code
    PortControlCreator creator;
    // general JNI variables
} ControlCreatorJNI;


DllExport void __stdcall PortMixer_nGetControls
    (UINT_PTR id, INT32 portIndex,
    PORT_NewBooleanControlPtr PORT_NewBooleanControl,
    PORT_NewCompoundControlPtr PORT_NewCompoundControl,
    PORT_NewFloatControlPtr PORT_NewFloatControl,
    PORT_AddControlPtr PORT_AddControl) {

    ControlCreatorJNI creator;

#if USE_PORTS == TRUE
    if (id != 0) {
    memset(&creator, 0, sizeof(ControlCreatorJNI));
    creator.creator.newBooleanControl  = PORT_NewBooleanControl;
    creator.creator.newCompoundControl = PORT_NewCompoundControl;
    creator.creator.newFloatControl    = PORT_NewFloatControl;
    creator.creator.addControl         = PORT_AddControl;
    PORT_GetControls((void*) (UINT_PTR) id, (INT32) portIndex, (PortControlCreator*) &creator);
    }
#endif
}

