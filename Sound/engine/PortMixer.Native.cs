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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Globalization;
using SystemX.Sound.Sampled;
using Windows.Win32.Foundation;

namespace SystemX.Media.Sound {
    partial class PortMixer {

        //class PortMixerHelper {

        //    private static readonly List<Control> s_tmpCtrls = new List<Control>();
        //    private static List<Control> s_ControlsList;

        //    public PortMixerHelper(List<Control> controlsList) {

        //    }

        //    private static IntPtr PORT_NewBoolCtrl(
        //        IntPtr creator,
        //        IntPtr controlID,
        //        IntPtr type) {
        //        String name;
        //        int result = 0;
        //        try {
        //            if ((long)type == CONTROL_TYPE_MUTE) {
        //                name = "Mute";
        //            } else if ((long)type == CONTROL_TYPE_SELECT) {
        //                name = "Select";
        //            } else
        //                name = Marshal.PtrToStringAnsi(type);
        //            if (String.IsNullOrEmpty(name))
        //                return (IntPtr)result;
        //            BoolCtrl ctrl = new BoolCtrl(controlID, name);
        //            s_tmpCtrls.Add(ctrl);
        //            result = s_tmpCtrls.Count;
        //        } catch (Exception) { }
        //        return (IntPtr)result;
        //    }

        //}

        private const int PORT_STRING_LENGTH = 200;

        // for BooleanControl.Type
        internal static readonly nint CONTROL_TYPE_MUTE = (1); //(char*)
        internal static readonly nint CONTROL_TYPE_SELECT = (2);

        // for FloatControl.Type
        internal static readonly nint CONTROL_TYPE_BALANCE = (1);
        internal static readonly nint CONTROL_TYPE_MASTER_GAIN = (2);
        internal static readonly nint CONTROL_TYPE_PAN = (3);
        internal static readonly nint CONTROL_TYPE_VOLUME = (4);
        internal const int CONTROL_TYPE_MAX = 4;


        private static readonly List<Control> s_tmpCtrls = new List<Control>();
        private static List<Control> s_ControlsList;

        internal delegate IntPtr DgBoolCtrl(
            PortControlCreatorPtr creator,
            PortControlIDPtr controlID,
            IntPtr type
        );
        internal delegate IntPtr DgCompCtrl(
            PortControlCreatorPtr creator,
            [MarshalAs(UnmanagedType.LPStr)]
            String name,
            Control[] controls
        );
        internal delegate IntPtr DgFloatCtrl(
            PortControlCreatorPtr creator,
            PortControlIDPtr controlID,
            IntPtr type,
            float min, float max, float precision,
            [MarshalAs(UnmanagedType.LPStr)]
            string units
        );
        internal delegate void DgAddCtrl(
            PortControlCreatorPtr creator,
            PortControlIDPtr controlID,
            IntPtr index);

        private static IntPtr PORT_NewBoolCtrl(
            PortControlCreatorPtr creator,
            PortControlIDPtr controlID,
            IntPtr type) {
            String name;
            int result = 0;
            try {
                if (type == CONTROL_TYPE_MUTE) {
                    name = "Mute";
                } else if (type == CONTROL_TYPE_SELECT) {
                    name = "Select";
                } else
                    name = Marshal.PtrToStringAnsi(type);
                if (String.IsNullOrEmpty(name))
                    return (IntPtr)result;
                BoolCtrl ctrl = new BoolCtrl(controlID, name);
                s_tmpCtrls.Add(ctrl);
                result = s_tmpCtrls.Count;
            } catch (Exception) { }
            return (IntPtr)result;
        }

        private static IntPtr PORT_NewCompCtrl(
            PortControlCreatorPtr creator,
            [MarshalAs(UnmanagedType.LPStr)]
            String name,
            Control[] controls) {
            int result = 0;
            if (String.IsNullOrEmpty(name))
                return (IntPtr)result;

            Control[] controlsArray = new Control[s_tmpCtrls.Count];
            s_tmpCtrls.CopyTo(controlsArray, 0);
            CompCtrl ctrl = new CompCtrl(name, controlsArray);
            s_tmpCtrls.Clear();
            s_tmpCtrls.Add(ctrl);
            result = s_tmpCtrls.Count;
            return (IntPtr)result;
        }

        private static IntPtr PORT_NewFloatCtrl(
            PortControlCreatorPtr creator,
            PortControlIDPtr controlID,
            IntPtr type,
            float min, float max, float precision,
            [MarshalAs(UnmanagedType.LPStr)]
            string units) {
            int result = 0;
            FloatCtrl ctrl;
            try {
                if ((long)type <= CONTROL_TYPE_MAX) {
                    ctrl = new FloatCtrl(controlID, (int)type, min, max, precision, units);
                } else {
                    String name = Marshal.PtrToStringAnsi(type);
                    if (String.IsNullOrEmpty(name))
                        return (IntPtr)result;
                    ctrl = new FloatCtrl(controlID, name, min, max, precision, units);
                }
                s_tmpCtrls.Add(ctrl);
                result = s_tmpCtrls.Count;
            } catch (Exception) { }
            ;
            return (IntPtr)result;
        }

        private static void PORT_AddCtrl(
            PortControlCreatorPtr creator,
            PortControlIDPtr controlID,
            IntPtr ctrlNo) {
            if ((long)ctrlNo > 0 && (long)ctrlNo <= s_tmpCtrls.Count) {
                s_ControlsList.Add(s_tmpCtrls[(int)ctrlNo - 1]);
            }
            if ((int)ctrlNo >= s_tmpCtrls.Count) {
                s_tmpCtrls.Clear();
            }
        }


        // open the mixer with the given index. Returns a handle ID
        //Object = PortInfoHandle
        private static PortInfoPtr nOpen(int mixerIndex) {
            return NativeMethods.PortMixer_nOpen(mixerIndex);
        }

        private static void nClose(PortInfoPtr id) {
            NativeMethods.PortMixer_nClose(id);
        }

        // gets the number of ports for this mixer
        private static int nGetPortCount(PortInfoPtr id) {
            return NativeMethods.PortMixer_nGetPortCount(id);
        }

        // gets the type of the port with this index
        private static int nGetPortType(PortInfoPtr id, int portIndex) {
            return NativeMethods.PortMixer_nGetPortType(id, portIndex);
        }

        // gets the name of the port with this index
        private static unsafe String nGetPortName(PortInfoPtr id, int portIndex) {
            byte* bytes = stackalloc byte[PORT_STRING_LENGTH];
            PUTF8STR utf8Str = new PUTF8STR(bytes);
            NativeMethods.PortMixer_nGetPortName(id, portIndex, utf8Str);
            return utf8Str.ToString();
        }

        // fills the vector with the controls for this port
        //private static void nGetControls(IntPtr id, int portIndex, List<Control> vector) {
        //    ctrls = vector;
        //    AddCtrlPtr addCtrl = AddCtrlFunc;
        //    BoolCtrlPtr boolCtrl = BoolCtrlFunc;
        //    CompCtrlPtr compCtrl = CompCtrlFunc;
        //    FloatCtrlPtr floatCtrl = FloatCtrlFunc;
        //    NativeMethods.PortMixer_InitCallbacks(addCtrl, boolCtrl, compCtrl, floatCtrl);
        //    NativeMethods.PortMixer_nGetControls(id, portIndex);
        //    GC.KeepAlive(addCtrl);
        //    GC.KeepAlive(boolCtrl);
        //    GC.KeepAlive(compCtrl);
        //    GC.KeepAlive(floatCtrl);
        //}

        private static unsafe void nGetControls(PortInfoPtr id, int portIndex, List<Control> vector) {
            s_ControlsList = new List<Control>();
            s_tmpCtrls.Clear();
            DgAddCtrl addCtrl = PORT_AddCtrl;
            //IntPtr naddCtrl = Marshal.GetFunctionPointerForDelegate(addCtrl);
            //(delegate* unmanaged[Stdcall]<PortControlCreatorPtr, PortControlIDPtr, IntPtr, void>)

            DgBoolCtrl boolCtrl = PORT_NewBoolCtrl;
            //IntPtr nboolCtrl = Marshal.GetFunctionPointerForDelegate(boolCtrl);
            //(delegate* unmanaged[Stdcall]<PortControlCreatorPtr, PortControlIDPtr, IntPtr, IntPtr>)

            DgCompCtrl compCtrl = PORT_NewCompCtrl;
            //IntPtr ncompCtrl = Marshal.GetFunctionPointerForDelegate(compCtrl);
            //(delegate* unmanaged[Stdcall]<PortControlCreatorPtr, PortControlIDPtr, IntPtr, IntPtr>)

            DgFloatCtrl floatCtrl = PORT_NewFloatCtrl;
            //IntPtr nfloatCtrl = Marshal.GetFunctionPointerForDelegate(floatCtrl);
            //(delegate* unmanaged[Stdcall]<PortControlCreatorPtr, PortControlIDPtr, IntPtr, float, float, float, PSTR, IntPtr>)

            NativeMethods.PortMixer_nGetControls(id, portIndex, boolCtrl, compCtrl, floatCtrl, addCtrl);
            for (int i = 0; i < s_ControlsList.Count; i++)
                vector.Add(s_ControlsList[i]);
        }

        // getters/setters for controls
        //Object = PortControlID
        private static void nControlSetIntValue(PortControlIDPtr controlID, int value) {
            NativeMethods.PortMixer_nControlSetIntValue(controlID, value);
        }

        private static int nControlGetIntValue(PortControlIDPtr controlID) {
            return NativeMethods.PortMixer_nControlGetIntValue(controlID);
        }

        private static void nControlSetFloatValue(PortControlIDPtr controlID, float value) {
            NativeMethods.PortMixer_nControlSetFloatValue(controlID, value);
        }

        private static float nControlGetFloatValue(PortControlIDPtr controlID) {
            return NativeMethods.PortMixer_nControlGetFloatValue(controlID);
        }

        private class NativeMethods {
            private const String CSound = "CSound.dll";

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern PortInfoPtr PortMixer_nOpen(int mixerIndex);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void PortMixer_nClose(PortInfoPtr id);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern int PortMixer_nGetPortCount(PortInfoPtr id);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern int PortMixer_nGetPortType(PortInfoPtr id, int portIndex);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void PortMixer_nGetPortName(PortInfoPtr id,
                int portIndex,
                PUTF8STR uft8Str);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void PortMixer_nGetControls(PortInfoPtr id, int portIndex);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            internal static extern void PortMixer_nGetControls(PortInfoPtr id, int portIndex,
            [MarshalAs(UnmanagedType.FunctionPtr)] DgBoolCtrl boolCtrl,
            [MarshalAs(UnmanagedType.FunctionPtr)] DgCompCtrl compCtrl,
            [MarshalAs(UnmanagedType.FunctionPtr)] DgFloatCtrl floatCtrl,
            [MarshalAs(UnmanagedType.FunctionPtr)] DgAddCtrl addCtrl);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void PortMixer_nControlSetIntValue(PortControlIDPtr controlID, int value);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern int PortMixer_nControlGetIntValue(PortControlIDPtr controlID);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void PortMixer_nControlSetFloatValue(PortControlIDPtr controlID, float value);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern float PortMixer_nControlGetFloatValue(PortControlIDPtr controlID);

        }
    }
}
