/*
 * Copyright (c) 2007, Oracle and/or its affiliates. All rights reserved.
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

//import java.util.ArrayList;
//import java.util.HashMap;
//import java.util.List;
//import java.util.Map;

using System;
using System.Collections.Generic;
using System.Text;

namespace SystemX.Media.Sound {

/**
 * Soundfont general region.
 *
 * @author Karl Helgason
 */
    public class SF2Region {

        public const int GENERATOR_STARTADDRSOFFSET = 0;
        public const int GENERATOR_ENDADDRSOFFSET = 1;
        public const int GENERATOR_STARTLOOPADDRSOFFSET = 2;
        public const int GENERATOR_ENDLOOPADDRSOFFSET = 3;
        public const int GENERATOR_STARTADDRSCOARSEOFFSET = 4;
        public const int GENERATOR_MODLFOTOPITCH = 5;
        public const int GENERATOR_VIBLFOTOPITCH = 6;
        public const int GENERATOR_MODENVTOPITCH = 7;
        public const int GENERATOR_INITIALFILTERFC = 8;
        public const int GENERATOR_INITIALFILTERQ = 9;
        public const int GENERATOR_MODLFOTOFILTERFC = 10;
        public const int GENERATOR_MODENVTOFILTERFC = 11;
        public const int GENERATOR_ENDADDRSCOARSEOFFSET = 12;
        public const int GENERATOR_MODLFOTOVOLUME = 13;
        public const int GENERATOR_UNUSED1 = 14;
        public const int GENERATOR_CHORUSEFFECTSSEND = 15;
        public const int GENERATOR_REVERBEFFECTSSEND = 16;
        public const int GENERATOR_PAN = 17;
        public const int GENERATOR_UNUSED2 = 18;
        public const int GENERATOR_UNUSED3 = 19;
        public const int GENERATOR_UNUSED4 = 20;
        public const int GENERATOR_DELAYMODLFO = 21;
        public const int GENERATOR_FREQMODLFO = 22;
        public const int GENERATOR_DELAYVIBLFO = 23;
        public const int GENERATOR_FREQVIBLFO = 24;
        public const int GENERATOR_DELAYMODENV = 25;
        public const int GENERATOR_ATTACKMODENV = 26;
        public const int GENERATOR_HOLDMODENV = 27;
        public const int GENERATOR_DECAYMODENV = 28;
        public const int GENERATOR_SUSTAINMODENV = 29;
        public const int GENERATOR_RELEASEMODENV = 30;
        public const int GENERATOR_KEYNUMTOMODENVHOLD = 31;
        public const int GENERATOR_KEYNUMTOMODENVDECAY = 32;
        public const int GENERATOR_DELAYVOLENV = 33;
        public const int GENERATOR_ATTACKVOLENV = 34;
        public const int GENERATOR_HOLDVOLENV = 35;
        public const int GENERATOR_DECAYVOLENV = 36;
        public const int GENERATOR_SUSTAINVOLENV = 37;
        public const int GENERATOR_RELEASEVOLENV = 38;
        public const int GENERATOR_KEYNUMTOVOLENVHOLD = 39;
        public const int GENERATOR_KEYNUMTOVOLENVDECAY = 40;
        public const int GENERATOR_INSTRUMENT = 41;
        public const int GENERATOR_RESERVED1 = 42;
        public const int GENERATOR_KEYRANGE = 43;
        public const int GENERATOR_VELRANGE = 44;
        public const int GENERATOR_STARTLOOPADDRSCOARSEOFFSET = 45;
        public const int GENERATOR_KEYNUM = 46;
        public const int GENERATOR_VELOCITY = 47;
        public const int GENERATOR_INITIALATTENUATION = 48;
        public const int GENERATOR_RESERVED2 = 49;
        public const int GENERATOR_ENDLOOPADDRSCOARSEOFFSET = 50;
        public const int GENERATOR_COARSETUNE = 51;
        public const int GENERATOR_FINETUNE = 52;
        public const int GENERATOR_SAMPLEID = 53;
        public const int GENERATOR_SAMPLEMODES = 54;
        public const int GENERATOR_RESERVED3 = 55;
        public const int GENERATOR_SCALETUNING = 56;
        public const int GENERATOR_EXCLUSIVECLASS = 57;
        public const int GENERATOR_OVERRIDINGROOTKEY = 58;
        public const int GENERATOR_UNUSED5 = 59;
        public const int GENERATOR_ENDOPR = 60;
        protected internal IDictionary<Int32, Int16> generators = new Dictionary<Int32, Int16>();
        protected internal IList<SF2Modulator> modulators = new List<SF2Modulator>();

        public IDictionary<Int32, Int16> getGenerators() {
            return generators;
        }

        public bool contains(int generator) {
            return generators.ContainsKey(generator);
        }

        public static short getDefaultValue(int generator) {
            if (generator == 8) return (short)13500;
            if (generator == 21) return (short)-12000;
            if (generator == 23) return (short)-12000;
            if (generator == 25) return (short)-12000;
            if (generator == 26) return (short)-12000;
            if (generator == 27) return (short)-12000;
            if (generator == 28) return (short)-12000;
            if (generator == 30) return (short)-12000;
            if (generator == 33) return (short)-12000;
            if (generator == 34) return (short)-12000;
            if (generator == 35) return (short)-12000;
            if (generator == 36) return (short)-12000;
            if (generator == 38) return (short)-12000;
            if (generator == 43) return (short)0x7F00;
            if (generator == 44) return (short)0x7F00;
            if (generator == 46) return (short)-1;
            if (generator == 47) return (short)-1;
            if (generator == 56) return (short)100;
            if (generator == 58) return (short)-1;
            return 0;
        }

        public short getShort(int generator) {
            if (!contains(generator))
                return getDefaultValue(generator);
            return generators[generator];
        }

        public void putShort(int generator, short value) {
            generators[generator] = value;
        }

        public byte[] getBytes(int generator) {
            int val = getInteger(generator);
            byte[] bytes = new byte[2];
            bytes[0] = (byte)(0xFF & val);
            bytes[1] = (byte)((0xFF00 & val) >> 8);
            return bytes;
        }

        public void putBytes(int generator, byte[] bytes) {
            generators[generator] = (short)(bytes[0] + (bytes[1] << 8));
        }

        public int getInteger(int generator) {
            return 0xFFFF & getShort(generator);
        }

        public void putInteger(int generator, int value) {
            generators[generator] = (short)value;
        }

        public IList<SF2Modulator> getModulators() {
            return modulators;
        }
    }
}
