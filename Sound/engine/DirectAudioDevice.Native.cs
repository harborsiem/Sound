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
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Security;
using System.Runtime.InteropServices;
using System.Globalization;
using SystemX.Sound.Sampled;
using Windows.Win32.Foundation;

namespace SystemX.Media.Sound {
    partial class DirectAudioDevice {

        internal delegate void AddAudioFormat(
            int significantBits, int frameSizeInBytes,
            int channels, float sampleRate,
            int encoding, BOOL isSigned,
            BOOL bigEndian);
        static IList<AudioFormat> oformats;

        public static void DAUDIO_AddAudioFormat(
            int significantBits, int frameSizeInBytes,
            int channels, float sampleRate,
            int encoding, BOOL isSigned,
            BOOL bigEndian) {

            if (frameSizeInBytes <= 0) {
                if (channels > 0) {
                    frameSizeInBytes = ((significantBits + 7) / 8) * channels;
                } else {
                    frameSizeInBytes = -1;
                }
            }
            Debug.WriteLine(String.Format(CultureInfo.InvariantCulture, "AddAudioFormat with sigBits={0} bits, frameSize={1} bytes, channels={2}, sampleRate={3} ",
               significantBits, frameSizeInBytes, channels, (int)sampleRate));
            Debug.WriteLine(String.Format(CultureInfo.InvariantCulture, "enc={0}, signed={1}, bigEndian={2}\n", encoding, isSigned, bigEndian));
            IList<AudioFormat> formats = (IList<AudioFormat>)oformats;
            addFormat(formats, significantBits, frameSizeInBytes,
                              channels, sampleRate, encoding, isSigned, bigEndian);
        }

        //Object = DAUDIO_Info
        private static void nGetFormats(int mixerIndex, int deviceID,
                           bool isSource, IList<AudioFormat> formats) {
            oformats = formats;
            NativeMethods.DirectAudioDevice_nGetFormats(mixerIndex, deviceID,
                isSource, DAUDIO_AddAudioFormat);
        }

        //return: DAUDIO_Info
        private static DAUDIO_InfoPtr nOpen(int mixerIndex, int deviceID, bool isSource,
                         int encoding,
                         float sampleRate,
                         int sampleSizeInBits,
                         int frameSize,
                         int channels,
                         bool signed,
                         bool bigEndian,
                         int bufferSize) {
            return NativeMethods.DirectAudioDevice_nOpen(mixerIndex, deviceID, isSource,
            encoding,
            sampleRate,
            sampleSizeInBits,
            frameSize,
            channels,
            signed,
            bigEndian,
            bufferSize);
        }

        private static void nStart(DAUDIO_InfoPtr id, bool isSource) {
            NativeMethods.DirectAudioDevice_nStart(id, isSource);
        }

        private static void nStop(DAUDIO_InfoPtr id, bool isSource) {
            NativeMethods.DirectAudioDevice_nStop(id, isSource);
        }

        private static void nClose(DAUDIO_InfoPtr id, bool isSource) {
            NativeMethods.DirectAudioDevice_nClose(id, isSource);
        }

        private static int nWrite(DAUDIO_InfoPtr id, byte[] b, int off, int len, int conversionSize,
                                         float volLeft, float volRight) {
            return NativeMethods.DirectAudioDevice_nWrite(
                id, b, off, len, conversionSize,
                volLeft, volRight);
        }

        private static int nRead(DAUDIO_InfoPtr id, byte[] b, int off, int len, int conversionSize) {
            return NativeMethods.DirectAudioDevice_nRead(
                id, b, off, len, conversionSize);
        }

        private static int nGetBufferSize(DAUDIO_InfoPtr id, bool isSource) {
            return NativeMethods.DirectAudioDevice_nGetBufferSize(id, isSource);
        }

        private static bool nIsStillDraining(DAUDIO_InfoPtr id, bool isSource) {
            return NativeMethods.DirectAudioDevice_nIsStillDraining(id, isSource);
        }

        private static void nFlush(DAUDIO_InfoPtr id, bool isSource) {
            NativeMethods.DirectAudioDevice_nFlush(id, isSource);
        }

        private static int nAvailable(DAUDIO_InfoPtr id, bool isSource) {
            return NativeMethods.DirectAudioDevice_nAvailable(id, isSource);
        }

        // javaPos is number of bytes read/written in Java layer
        private static long nGetBytePosition(DAUDIO_InfoPtr id, bool isSource, long javaPos) {
            return NativeMethods.DirectAudioDevice_nGetBytePosition(id, isSource, javaPos);
        }

        private static void nSetBytePosition(DAUDIO_InfoPtr id, bool isSource, long pos) {
            NativeMethods.DirectAudioDevice_nSetBytePosition(id, isSource, pos);
        }

        // returns if the native implementation needs regular calls to nService()
        private static bool nRequiresServicing(DAUDIO_InfoPtr id, bool isSource) {
            return NativeMethods.DirectAudioDevice_nRequiresServicing(id, isSource);
        }

        // called in irregular intervals
        private static void nService(DAUDIO_InfoPtr id, bool isSource) {
            NativeMethods.DirectAudioDevice_nService(id, isSource);
        }

        private class NativeMethods {

            //, Sound.dll, Version=4.0.0.0, Culture=neutral, PublicKeyToken=987df3c2e7e93158
            private const String CSound = "CSound.dll";

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void DirectAudioDevice_nGetFormats(
                int mixerIndex,
                int deviceID,
                BOOL isSource,
                AddAudioFormat addFormat);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern DAUDIO_InfoPtr DirectAudioDevice_nOpen(
                int mixerIndex,
                int deviceID,
                BOOL isSource,
                int encoding,
                float sampleRate,
                int sampleSizeInBits,
                int frameSize,
                int channels,
                BOOL signed,
                BOOL bigEndian,
                int bufferSize);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void DirectAudioDevice_nStart(
                DAUDIO_InfoPtr id,
                BOOL isSource);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void DirectAudioDevice_nStop(
                DAUDIO_InfoPtr id,
                BOOL isSource);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void DirectAudioDevice_nClose(
                DAUDIO_InfoPtr id,
                BOOL isSource);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern int DirectAudioDevice_nWrite(
                DAUDIO_InfoPtr id,
                [In, Out] byte[] b,
                int off,
                int len,
                int conversionSize,
                float volLeft,
                float volRight);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern int DirectAudioDevice_nRead(
                DAUDIO_InfoPtr id,
                [In, Out] byte[] b,
                int off,
                int len,
                int conversionSize);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern int DirectAudioDevice_nGetBufferSize(
                DAUDIO_InfoPtr id,
                BOOL isSource);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern BOOL DirectAudioDevice_nIsStillDraining(
                DAUDIO_InfoPtr id,
                BOOL isSource);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void DirectAudioDevice_nFlush(
                DAUDIO_InfoPtr id,
                BOOL isSource);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern int DirectAudioDevice_nAvailable(
                DAUDIO_InfoPtr id,
                BOOL isSource);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern long DirectAudioDevice_nGetBytePosition(
                DAUDIO_InfoPtr id,
                BOOL isSource,
                long javaPos);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void DirectAudioDevice_nSetBytePosition(
                DAUDIO_InfoPtr id,
                BOOL isSource,
                long pos);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern BOOL DirectAudioDevice_nRequiresServicing(
                DAUDIO_InfoPtr id,
                BOOL isSource);

            [DllImport(CSound, CharSet = CharSet.Ansi)]
            public static extern void DirectAudioDevice_nService(
                DAUDIO_InfoPtr id,
                BOOL isSource);
        }
    }
}
