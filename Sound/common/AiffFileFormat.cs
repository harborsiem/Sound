/*
 * Copyright (c) 1999, 2016, Oracle and/or its affiliates. All rights reserved.
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

//import javax.sound.sampled.AudioFormat;

using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * AIFF file format.
 *
 * @author Jan Borgersen
 */
    internal sealed class AiffFileFormat : StandardFileFormat {

        internal const int AIFF_MAGIC = 1179603533;

        // for writing AIFF
        internal const int AIFC_MAGIC = 0x41494643; // 'AIFC'
        internal const int AIFF_MAGIC2 = 0x41494646;    // 'AIFF'
        internal const int FVER_MAGIC = 0x46564552; // 'FVER'
        internal const int FVER_TIMESTAMP = unchecked((int)0xA2805140); // timestamp of last AIFF-C update
        internal const int COMM_MAGIC = 0x434f4d4d; // 'COMM'
        internal const int SSND_MAGIC = 0x53534e44; // 'SSND'

        // compression codes
        internal const int AIFC_PCM = 0x4e4f4e45;   // 'NONE' PCM
        internal const int AIFC_ACE2 = 0x41434532;  // 'ACE2' ACE 2:1 compression
        internal const int AIFC_ACE8 = 0x41434538;  // 'ACE8' ACE 8:3 compression
        internal const int AIFC_MAC3 = 0x4d414333;  // 'MAC3' MACE 3:1 compression
        internal const int AIFC_MAC6 = 0x4d414336;  // 'MAC6' MACE 6:1 compression
        internal const int AIFC_ULAW = 0x756c6177;  // 'ulaw' ITU G.711 u-Law
        internal const int AIFC_IMA4 = 0x696d6134;  // 'ima4' IMA ADPCM

        // $$fb static approach not good, but needed for estimation
        internal const int AIFF_HEADERSIZE = 54;

        //$$fb 2001-07-13: added management of header size in this class

        /** Header size in bytes */
        private readonly int headerSize = AIFF_HEADERSIZE;

        /** comm chunk size in bytes, inclusive magic and length field */
        private readonly int commChunkSize = 26;

        /** FVER chunk size in bytes, inclusive magic and length field */
        private readonly int fverChunkSize = 0;

        internal AiffFileFormat(Type type, long byteLength, AudioFormat format, long frameLength)
            : base(type, byteLength, format, frameLength) {
        }

        internal int getHeaderSize() {
            return headerSize;
        }

        internal int getCommChunkSize() {
            return commChunkSize;
        }

        internal int getFverChunkSize() {
            return fverChunkSize;
        }

        internal int getSsndChunkOffset() {
            return getHeaderSize() - 16;
        }
    }
}
