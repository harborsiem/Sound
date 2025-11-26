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

//import javax.sound.sampled.AudioFileFormat;
//import javax.sound.sampled.AudioFormat;

using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * WAVE file format class.
 *
 * @author Jan Borgersen
 */
    internal sealed class WaveFileFormat : StandardFileFormat {

        /**
         * Wave format type.
         */
        private readonly int waveType;

        //$$fb 2001-07-13: added management of header size in this class
        //$$fb 2002-04-16: Fix for 4636355: RIFF audio headers could be _more_ spec compliant
        private const int STANDARD_HEADER_SIZE = 28;

        //$$fb 2002-04-16: Fix for 4636355: RIFF audio headers could be _more_ spec compliant
        /**
         * fmt_ chunk size in bytes.
         */
        private const int STANDARD_FMT_CHUNK_SIZE = 16;

        // magic numbers
        internal const int RIFF_MAGIC = 1380533830;
        internal const int WAVE_MAGIC = 1463899717;
        internal const int FMT_MAGIC = 0x666d7420; // "fmt "
        internal const int DATA_MAGIC = 0x64617461; // "data"

        // encodings
        internal const int WAVE_FORMAT_UNKNOWN = 0x0000;
        internal const int WAVE_FORMAT_PCM = 0x0001;
        internal const int WAVE_FORMAT_ADPCM = 0x0002;
        internal const int WAVE_FORMAT_IEEE_FLOAT = 0x0003;
        internal const int WAVE_FORMAT_ALAW = 0x0006;
        internal const int WAVE_FORMAT_MULAW = 0x0007;
        internal const int WAVE_FORMAT_OKI_ADPCM = 0x0010;
        internal const int WAVE_FORMAT_DIGISTD = 0x0015;
        internal const int WAVE_FORMAT_DIGIFIX = 0x0016;
        internal const int WAVE_IBM_FORMAT_MULAW = 0x0101;
        internal const int WAVE_IBM_FORMAT_ALAW = 0x0102;
        internal const int WAVE_IBM_FORMAT_ADPCM = 0x0103;
        internal const int WAVE_FORMAT_DVI_ADPCM = 0x0011;
        internal const int WAVE_FORMAT_SX7383 = 0x1C07;
        internal const int WAVE_FORMAT_EXTENSIBLE = 0xFFFE;

        internal WaveFileFormat(AudioFileFormat.Type type, long byteLength,
               AudioFormat format, long frameLength)
            : base(type, byteLength, format, frameLength) {

            AudioFormat.Encoding encoding = format.getEncoding();

            if (encoding.Equals(AudioFormat.Encoding.ALAW)) {
                waveType = WAVE_FORMAT_ALAW;
            } else if (encoding.Equals(AudioFormat.Encoding.ULAW)) {
                waveType = WAVE_FORMAT_MULAW;
            } else if (encoding.Equals(AudioFormat.Encoding.PCM_SIGNED) ||
                   encoding.Equals(AudioFormat.Encoding.PCM_UNSIGNED)) {
                waveType = WAVE_FORMAT_PCM;
            } else {
                waveType = WAVE_FORMAT_UNKNOWN;
            }
        }

        internal int getWaveType() {
            return waveType;
        }

        internal int getHeaderSize() {
            return getHeaderSize(getWaveType());
        }

        internal static int getHeaderSize(int waveType) {
            //$$fb 2002-04-16: Fix for 4636355: RIFF audio headers could be _more_ spec compliant
            // use dynamic format chunk size
            return STANDARD_HEADER_SIZE + getFmtChunkSize(waveType);
        }

        internal static int getFmtChunkSize(int waveType) {
            //$$fb 2002-04-16: Fix for 4636355: RIFF audio headers could be _more_ spec compliant
            // add 2 bytes for "codec specific data length" for non-PCM codecs
            int result = STANDARD_FMT_CHUNK_SIZE;
            if (waveType != WAVE_FORMAT_PCM) {
                result += 2; // WORD for "codec specific data length"
            }
            return result;
        }
    }
}

