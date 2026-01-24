#undef NoNative
//#define NoNative
/*
 * Copyright (c) 2002, 2021, Oracle and/or its affiliates. All rights reserved.
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

//import java.util.Vector;

//import javax.sound.sampled.BooleanControl;
//import javax.sound.sampled.CompoundControl;
//import javax.sound.sampled.Control;
//import javax.sound.sampled.FloatControl;
//import javax.sound.sampled.Line;
//import javax.sound.sampled.LineUnavailableException;
//import javax.sound.sampled.Port;

using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {
/**
 * A Mixer which only provides Ports.
 *
 * @author Florian Bomers
 */
    internal sealed partial class PortMixer : AbstractMixer {

        internal const int SRC_UNKNOWN = 0x01;
        internal const int SRC_MICROPHONE = 0x02;
        internal const int SRC_LINE_IN = 0x03;
        internal const int SRC_COMPACT_DISC = 0x04;
        internal const int SRC_MASK = 0xFF;

        internal const int DST_UNKNOWN = 0x0100;
        internal const int DST_SPEAKER = 0x0200;
        internal const int DST_HEADPHONE = 0x0300;
        internal const int DST_LINE_OUT = 0x0400;
        internal const int DST_MASK = 0xFF00;

        private readonly Port.Info[] portInfos;
        // cache of instantiated ports
        private PortMixerPort[] ports;

        // instance ID of the native implementation
        private PortInfoPtr id;

        internal PortMixer(PortMixerProvider.PortMixerInfo portMixerInfo)
            // pass in Line.Info, mixer, controls
            : base(portMixerInfo,             // Mixer.Info
                  null,                       // Control[]
                  null,                       // Line.Info[] sourceLineInfo
                  null) {                     // Line.Info[] targetLineInfo

            int count = 0;
            int srcLineCount = 0;
            int dstLineCount = 0;

            try {
                try {
                    id = nOpen(getMixerIndex());
                    if (!id.IsNull) {
                        count = nGetPortCount(id);
                        if (count < 0) {
                            count = 0;
                        }
                    }
                } catch (Exception) { }

                portInfos = new Port.Info[count];

                for (int i = 0; i < count; i++) {
                    int type = nGetPortType(id, i);
                    srcLineCount += ((type & SRC_MASK) != 0) ? 1 : 0;
                    dstLineCount += ((type & DST_MASK) != 0) ? 1 : 0;
                    portInfos[i] = getPortInfo(i, type);
                }
            } finally {
                if (!id.IsNull) {
                    nClose(id);
                }
                id = PortInfoPtr.Null;
            }

            // fill sourceLineInfo and targetLineInfos with copies of the ones in portInfos
            sourceLineInfo = new Port.Info[srcLineCount];
            targetLineInfo = new Port.Info[dstLineCount];

            srcLineCount = 0; dstLineCount = 0;
            for (int i = 0; i < count; i++) {
                if (portInfos[i].isSource()) {
                    sourceLineInfo[srcLineCount++] = portInfos[i];
                } else {
                    targetLineInfo[dstLineCount++] = portInfos[i];
                }
            }
        }

        public override ILine getLine(Line.Info info) {
            Line.Info fullInfo = getLineInfo(info);

            if (fullInfo is Port.Info) {
                for (int i = 0; i < portInfos.Length; i++) {
                    if (fullInfo.Equals(portInfos[i])) {
                        return getPort(i);
                    }
                }
            }
            throw new ArgumentException("Line unsupported: " + info);
        }

        public override int getMaxLines(Line.Info info) {
            Line.Info fullInfo = getLineInfo(info);

            // if it's not supported at all, return 0.
            if (fullInfo == null) {
                return 0;
            }

            if (fullInfo is Port.Info) {
                //return AudioSystem.NOT_SPECIFIED; // if several instances of PortMixerPort
                return 1;
            }
            return 0;
        }

        protected override void implOpen() {
            // open the mixer device
            id = nOpen(getMixerIndex());
        }

        protected override void implClose() {
            // close the mixer device
            PortInfoPtr thisID = id;
            id = PortInfoPtr.Null;
            nClose(thisID);
            if (ports != null) {
                for (int i = 0; i < ports.Length; i++) {
                    if (ports[i] != null) {
                        ports[i].disposeControls();
                    }
                }
            }
        }

        protected override void implStart() { }
        protected override void implStop() { }

        private Port.Info getPortInfo(int portIndex, int type) {
            switch (type) {
                case SRC_UNKNOWN: return new PortInfo(nGetPortName(getID(), portIndex), true);
                case SRC_MICROPHONE: return Port.Info.MICROPHONE;
                case SRC_LINE_IN: return Port.Info.LINE_IN;
                case SRC_COMPACT_DISC: return Port.Info.COMPACT_DISC;

                case DST_UNKNOWN: return new PortInfo(nGetPortName(getID(), portIndex), false);
                case DST_SPEAKER: return Port.Info.SPEAKER;
                case DST_HEADPHONE: return Port.Info.HEADPHONE;
                case DST_LINE_OUT: return Port.Info.LINE_OUT;
            }
            // should never happen...
            if (Printer.err) Printer.Err("unknown port type: " + type);
            return null;
        }

        int getMixerIndex() {
            return ((PortMixerProvider.PortMixerInfo)getMixerInfo()).getIndex();
        }

        IPort getPort(int index) {
            if (ports == null) {
                ports = new PortMixerPort[portInfos.Length];
            }
            if (ports[index] == null) {
                ports[index] = new PortMixerPort(portInfos[index], this, index);
                return ports[index];
            }
            // $$fb TODO: return (Port) (ports[index].clone());
            return ports[index];
        }

        internal PortInfoPtr getID() {
            return id;
        }

        /**
         * Private inner class representing a Port for the PortMixer.
         */
        private sealed class PortMixerPort : AbstractLine, IPort {
            private readonly int portIndex;
            private PortInfoPtr id;

            internal PortMixerPort(Port.Info info,
                                  PortMixer mixer,
                                  int portIndex)
                : base(info, mixer, null) {
                this.portIndex = portIndex;
            }

            void implOpen() {
                PortInfoPtr newID = ((PortMixer)mixer).getID();
                if ((id.IsNull) || (newID != id) || (controls.Length == 0)) {
                    id = newID;
                    List<Control> vector = new List<Control>(); //Vector
                    lock (vector) {
                        nGetControls(id, portIndex, vector);
                        controls = new Control[vector.Count];
                        for (int i = 0; i < controls.Length; i++) {
                            controls[i] = vector[i];
                        }
                    }
                } else {
                    enableControls(controls, true);
                }
            }

            private void enableControls(Control[] controls, bool enable) {
                for (int i = 0; i < controls.Length; i++) {
                    if (controls[i] is BoolCtrl) {
                        ((BoolCtrl)controls[i]).closed = !enable;
                    } else if (controls[i] is FloatCtrl) {
                        ((FloatCtrl)controls[i]).closed = !enable;
                    } else if (controls[i] is CompoundControl) {
                        enableControls(((CompoundControl)controls[i]).getMemberControls(), enable);
                    }
                }
            }

            internal void disposeControls() {
                enableControls(controls, false);
                controls = Array.Empty<Control>();
            }

            void implClose() {
                // get rid of controls
                enableControls(controls, false);
            }

            // this is very similar to open(AudioFormat, int) in AbstractDataLine...
            public override void open() {
                lock (mixer) {
                    // if the line is not currently open, try to open it with this format and buffer size
                    if (!isOpen()) {
                        // reserve mixer resources for this line
                        mixer.open(this);
                        try {
                            // open the line.  may throw LineUnavailableException.
                            implOpen();

                            // if we succeeded, set the open state to true and send events
                            setOpen(true);
                        } catch (LineUnavailableException) {
                            // release mixer resources for this line and then throw the exception
                            mixer.close(this);
                            throw;
                        }
                    }
                }
            }

            // this is very similar to close() in AbstractDataLine...
            public override void close() {
                lock (mixer) {
                    if (isOpen()) {
                        // set the open state to false and send events
                        setOpen(false);

                        // close resources for this line
                        implClose();

                        // release mixer resources for this line
                        mixer.close(this);
                    }
                }
            }

        } // class PortMixerPort

        /**
         * Private inner class representing a BooleanControl for PortMixerPort.
         */
        private sealed class BoolCtrl : BooleanControl {
            // the handle to the native control function
            private readonly PortControlIDPtr controlID;
            internal bool closed = false;

            internal static BooleanControl.Type createType(String name) {
                if (name.Equals("Mute")) {
                    return BooleanControl.Type.MUTE;
                } else if (name.Equals("Select")) {
                    // $$fb add as new static type?
                    //return BooleanControl.Type.SELECT;
                }
                return new BCT(name);
            }

            internal BoolCtrl(PortControlIDPtr controlID, String name)
                : this(controlID, createType(name)) {
            }

            internal BoolCtrl(PortControlIDPtr controlID, BooleanControl.Type typ)
                : base(typ, false) {
                this.controlID = controlID;
            }

            public override void setValue(bool value) {
                if (!closed) {
                    nControlSetIntValue(controlID, value ? 1 : 0);
                }
            }

            public override bool getValue() {
                if (!closed) {
                    // never use any cached values
                    return (nControlGetIntValue(controlID) != 0) ? true : false;
                }
                // ??
                return false;
            }

            /**
             * inner class for custom types.
             */
            private sealed class BCT : BooleanControl.Type {
                internal BCT(String name)
                    : base(name) {
                }
            }
        }

        /**
         * Private inner class representing a CompoundControl for PortMixerPort.
         */
        private sealed class CompCtrl : CompoundControl {
            internal CompCtrl(String name, Control[] controls)
                : base(new CCT(name), controls) {
            }

            /**
             * inner class for custom compound control types.
             */
            private sealed class CCT : CompoundControl.Type {
                internal CCT(String name)
                    : base(name) {
                }
            }
        }

        /**
         * Private inner class representing a BooleanControl for PortMixerPort.
         */
        private sealed class FloatCtrl : FloatControl {
            // the handle to the native control function
            private readonly PortControlIDPtr controlID;
            internal bool closed = false;

            // predefined float control types. See also Ports.h
            private static readonly FloatControl.Type[] FLOAT_CONTROL_TYPES = {
               null,
               FloatControl.Type.BALANCE,
               FloatControl.Type.MASTER_GAIN,
               FloatControl.Type.PAN,
               FloatControl.Type.VOLUME
           };

            internal FloatCtrl(PortControlIDPtr controlID, String name,
                              float min, float max, float precision, String units)
                : this(controlID, new FCT(name), min, max, precision, units) {
            }

            internal FloatCtrl(PortControlIDPtr controlID, int type,
                              float min, float max, float precision, String units)
                : this(controlID, FLOAT_CONTROL_TYPES[type], min, max, precision, units) {
            }

            internal FloatCtrl(PortControlIDPtr controlID, FloatControl.Type typ,
                             float min, float max, float precision, String units)
                : base(typ, min, max, precision, 1000, min, units) {
                this.controlID = controlID;
            }

            public override void setValue(float value) {
                if (!closed) {
                    nControlSetFloatValue(controlID, value);
                }
            }

            public override float getValue() {
                if (!closed) {
                    // never use any cached values
                    return nControlGetFloatValue(controlID);
                }
                // ??
                return getMinimum();
            }

            /**
             * inner class for custom types.
             */
            private sealed class FCT : FloatControl.Type {
                internal FCT(String name)
                    : base(name) {
                }
            }
        }

        /**
         * Private inner class representing a port info.
         */
        private sealed class PortInfo : Port.Info {
            internal PortInfo(String name, bool isSource)
                : base(typeof(IPort), name, isSource) {
            }
        }

#if NoNative
        // open the mixer with the given index. Returns a handle ID
        //Object = PortInfoHandle
        private static Object nOpen(int mixerIndex) { return null; }
        private static void nClose(Object id) { }

        // gets the number of ports for this mixer
        private static int nGetPortCount(Object id) { return 0; }

        // gets the type of the port with this index
        private static int nGetPortType(Object id, int portIndex) { return 0; }

        // gets the name of the port with this index
        private static String nGetPortName(Object id, int portIndex) { return String.Empty; }

        // fills the vector with the controls for this port
        private static void nGetControls(Object id, int portIndex, List<Control> vector) { } //Vector

        // getters/setters for controls
        //Object = PortControlID
        private static void nControlSetIntValue(Object controlID, int value) { }
        private static int nControlGetIntValue(Object controlID) { return 0; }
        private static void nControlSetFloatValue(Object controlID, float value) { }
        private static float nControlGetFloatValue(Object controlID) { return 0f; }
#endif
    }
}
