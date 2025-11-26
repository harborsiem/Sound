/*
 * Copyright (c) 1999, 2021, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.IOException;


//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioFormat.Encoding;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;

using System;
using System.Collections.Generic;
using System.Text;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {
/**
 * A-law encodes linear data, and decodes a-law data to linear data.
 *
 * @author Kara Kytle
 */
    public sealed class AlawCodec : FormatConversionProvider {

        /* Tables used for A-law decoding */

        private static readonly byte[] ALAW_TABH = new byte[256];
        private static readonly byte[] ALAW_TABL = new byte[256];

        private static readonly short[] seg_end = {
            0xFF, 0x1FF, 0x3FF, 0x7FF, 0xFFF, 0x1FFF, 0x3FFF, 0x7FFF
        };

        /**
         * Initializes the decode tables.
         */
        static AlawCodec() {
            for (int i = 0; i < 256; i++) {
                int input = i ^ 0x55;
                int mantissa = (input & 0xf) << 4;
                int segment = (input & 0x70) >> 4;
                int value = mantissa + 8;

                if (segment >= 1)
                    value += 0x100;
                if (segment > 1)
                    value <<= (segment - 1);

                if ((input & 0x80) == 0)
                    value = -value;

                ALAW_TABL[i] = (byte)value;
                ALAW_TABH[i] = (byte)(value >> 8);
            }
        }

        public override AudioFormat.Encoding[] getSourceEncodings() {
            return new AudioFormat.Encoding[] { AudioFormat.Encoding.ALAW, AudioFormat.Encoding.PCM_SIGNED };
        }

        public override AudioFormat.Encoding[] getTargetEncodings() {
            return getSourceEncodings();
        }

        public override AudioFormat.Encoding[] getTargetEncodings(AudioFormat sourceFormat) {

            if (sourceFormat.getEncoding().Equals(AudioFormat.Encoding.PCM_SIGNED)) {

                if (sourceFormat.getSampleSizeInBits() == 16) {

                    AudioFormat.Encoding[] enc = new AudioFormat.Encoding[1];
                    enc[0] = AudioFormat.Encoding.ALAW;
                    return enc;

                } else {
                    return new AudioFormat.Encoding[0];
                }
            } else if (sourceFormat.getEncoding().Equals(AudioFormat.Encoding.ALAW)) {

                if (sourceFormat.getSampleSizeInBits() == 8) {

                    AudioFormat.Encoding[] enc = new AudioFormat.Encoding[1];
                    enc[0] = AudioFormat.Encoding.PCM_SIGNED;
                    return enc;

                } else {
                    return new AudioFormat.Encoding[0];
                }

            } else {
                return new AudioFormat.Encoding[0];
            }
        }

        public override AudioFormat[] getTargetFormats(AudioFormat.Encoding targetEncoding, AudioFormat sourceFormat) {
            if (sourceFormat == null)
                throw new ArgumentNullException(nameof(sourceFormat));
            if ((targetEncoding.Equals(AudioFormat.Encoding.PCM_SIGNED) && sourceFormat.getEncoding().Equals(AudioFormat.Encoding.ALAW)) ||
                (targetEncoding.Equals(AudioFormat.Encoding.ALAW) && sourceFormat.getEncoding().Equals(AudioFormat.Encoding.PCM_SIGNED))) {
                return getOutputFormats(sourceFormat);
            } else {
                return new AudioFormat[0];
            }
        }

        public override AudioInputStream getAudioInputStream(AudioFormat.Encoding targetEncoding, AudioInputStream sourceStream) {
            AudioFormat sourceFormat = sourceStream.getFormat();
            AudioFormat.Encoding sourceEncoding = sourceFormat.getEncoding();

            if (!isConversionSupported(targetEncoding, sourceStream.getFormat())) {
                throw new ArgumentException("Unsupported conversion: " + sourceStream.getFormat().ToString() + " to " + targetEncoding.ToString());
            }
            if (sourceEncoding.Equals(targetEncoding)) {
                return sourceStream;
            }
            AudioFormat targetFormat = null;
            if (sourceEncoding.Equals(AudioFormat.Encoding.ALAW) &&
            targetEncoding.Equals(AudioFormat.Encoding.PCM_SIGNED)) {

                targetFormat = new AudioFormat(targetEncoding,
                                sourceFormat.getSampleRate(),
                                16,
                                sourceFormat.getChannels(),
                                2 * sourceFormat.getChannels(),
                                sourceFormat.getSampleRate(),
                                sourceFormat.isBigEndian());

            } else if (sourceEncoding.Equals(AudioFormat.Encoding.PCM_SIGNED) &&
                   targetEncoding.Equals(AudioFormat.Encoding.ALAW)) {

                targetFormat = new AudioFormat(targetEncoding,
                                sourceFormat.getSampleRate(),
                                8,
                                sourceFormat.getChannels(),
                                sourceFormat.getChannels(),
                                sourceFormat.getSampleRate(),
                                false);
            } else {
                throw new ArgumentException("Unsupported conversion: " + sourceStream.getFormat().ToString() + " to " + targetEncoding.ToString());
            }
            return getConvertedStream(targetFormat, sourceStream);
        }

        public override AudioInputStream getAudioInputStream(AudioFormat targetFormat, AudioInputStream sourceStream) {
            if (!isConversionSupported(targetFormat, sourceStream.getFormat()))
                throw new ArgumentException("Unsupported conversion: "
                                                   + sourceStream.getFormat().ToString() + " to "
                                                   + targetFormat.ToString());
            return getConvertedStream(targetFormat, sourceStream);
        }

        /**
         * Opens the codec with the specified parameters.
         * @param stream stream from which data to be processed should be read
         * @param outputFormat desired data format of the stream after processing
         * @return stream from which processed data may be read
         * @throws IllegalArgumentException if the format combination supplied is
         * not supported.
         */
        private AudioInputStream getConvertedStream(AudioFormat outputFormat, AudioInputStream stream) {

            AudioInputStream cs = null;
            AudioFormat inputFormat = stream.getFormat();

            if (inputFormat.matches(outputFormat)) {

                cs = stream;
            } else {

                cs = new AlawCodecStream(this, stream, outputFormat);
            }

            return cs;
        }

        /**
         * Obtains the set of output formats supported by the codec
         * given a particular input format.
         * If no output formats are supported for this input format,
         * returns an array of length 0.
         * @return array of supported output formats.
         */
        private AudioFormat[] getOutputFormats(AudioFormat inputFormat) {

            List<AudioFormat> formats = new List<AudioFormat>();
            AudioFormat format;

            if (inputFormat.getSampleSizeInBits() == 16
                && AudioFormat.Encoding.PCM_SIGNED.Equals(inputFormat.getEncoding())) {
                format = new AudioFormat(AudioFormat.Encoding.ALAW,
                             inputFormat.getSampleRate(), 8,
                             inputFormat.getChannels(),
                             inputFormat.getChannels(),
                             inputFormat.getSampleRate(), false);
                formats.Add(format);
            }

            if (inputFormat.getSampleSizeInBits() == 8
                && AudioFormat.Encoding.ALAW.Equals(inputFormat.getEncoding())) {
                format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                             inputFormat.getSampleRate(), 16,
                             inputFormat.getChannels(),
                             inputFormat.getChannels() * 2,
                             inputFormat.getSampleRate(), false);
                formats.Add(format);
                format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                             inputFormat.getSampleRate(), 16,
                             inputFormat.getChannels(),
                             inputFormat.getChannels() * 2,
                             inputFormat.getSampleRate(), true);
                formats.Add(format);
            }

            AudioFormat[] formatArray = new AudioFormat[formats.Count];
            for (int i = 0; i < formatArray.Length; i++) {
                formatArray[i] = formats[i];
            }
            return formatArray;
        }

        internal sealed class AlawCodecStream : AudioInputStream {

            private AlawCodec caller;

            // tempBuffer required only for encoding (when encode is true)
            private const int tempBufferSize = 64;
            private byte[] tempBuffer = null;

            /**
             * True to encode to a-law, false to decode to linear
             */
            bool encode = false;

            AudioFormat encodeFormat;
            AudioFormat decodeFormat;

            byte[] tabByte1 = null;
            byte[] tabByte2 = null;
            int highByte = 0;
            int lowByte = 1;

            internal AlawCodecStream(AlawCodec caller, AudioInputStream stream, AudioFormat outputFormat)

                : base(stream, outputFormat, -1) {

                this.caller = caller;

                AudioFormat inputFormat = stream.getFormat();

                // throw an IllegalArgumentException if not ok
                if (!(caller.isConversionSupported(outputFormat, inputFormat))) {

                    throw new ArgumentException("Unsupported conversion: " + inputFormat.ToString() + " to " + outputFormat.ToString());
                }

                //$$fb 2002-07-18: fix for 4714846: JavaSound ULAW (8-bit) encoder erroneously depends on endian-ness
                bool PCMIsBigEndian;

                // determine whether we are encoding or decoding
                if (AudioFormat.Encoding.ALAW.Equals(inputFormat.getEncoding())) {
                    encode = false;
                    encodeFormat = inputFormat;
                    decodeFormat = outputFormat;
                    PCMIsBigEndian = outputFormat.isBigEndian();
                } else {
                    encode = true;
                    encodeFormat = outputFormat;
                    decodeFormat = inputFormat;
                    PCMIsBigEndian = inputFormat.isBigEndian();
                    tempBuffer = new byte[tempBufferSize];
                }

                if (PCMIsBigEndian) {
                    tabByte1 = ALAW_TABH;
                    tabByte2 = ALAW_TABL;
                    highByte = 0;
                    lowByte = 1;
                } else {
                    tabByte1 = ALAW_TABL;
                    tabByte2 = ALAW_TABH;
                    highByte = 1;
                    lowByte = 0;
                }

                // set the AudioInputStream length in frames if we know it
                if (stream is AudioInputStream) {
                    frameLength = stream.getFrameLength();
                }

                // set framePos to zero
                framePos = 0;
                frameSize = inputFormat.getFrameSize();
                if (frameSize == AudioSystem.NOT_SPECIFIED) {
                    frameSize = 1;
                }
            }

            /*
             * $$jb 2/23/99
             * Used to determine segment number in aLaw encoding
             */
            private short search(short val, short[] table, short size) {
                for (short i = 0; i < size; i++) {
                    if (val <= table[i]) { return i; }
                }
                return size;
            }

            /**
             * Note that this won't actually read anything; must read in
             * two-byte units.
             */
            public override int ReadByte() {

                byte[] b = new byte[1];
                return Read(b, 0, b.Length);
            }


            //public override int read(byte[] b) {

            //    return Read(b, 0, b.Length);
            //}

            public override int Read(byte[] b, int off, int len) {

                // don't read fractional frames
                if (len % frameSize != 0) {
                    len -= (len % frameSize);
                }

                if (encode) {

                    short QUANT_MASK = 0xF;
                    short SEG_SHIFT = 4;
                    short mask;
                    short seg;
#pragma warning disable 0168
                    int adj;
#pragma warning restore 0168
                    int i;

                    short sample;
                    byte enc;

                    int readCount = 0;
                    int currentPos = off;
                    int readLeft = len * 2;
                    int readLen = ((readLeft > tempBufferSize) ? tempBufferSize : readLeft);

                    while ((readCount = base.Read(tempBuffer, 0, readLen)) > 0) {

                        for (i = 0; i < readCount; i += 2) {

                            /* Get the sample from the tempBuffer */
                            sample = (short)(((tempBuffer[i + highByte]) << 8) & 0xFF00);
#pragma warning disable 0675
                            sample |= (short)((tempBuffer[i + lowByte]) & 0xFF);
#pragma warning restore 0675

                            if (sample >= 0) {
                                mask = 0xD5;
                            } else {
                                mask = 0x55;
                                sample = (short)(-sample - 8);
                            }
                            /* Convert the scaled magnitude to segment number. */
                            seg = search(sample, seg_end, (short)8);
                            /*
                             * Combine the sign, segment, quantization bits
                             */
                            if (seg >= 8) {  /* out of range, return maximum value. */
                                enc = (byte)(0x7F ^ mask);
                            } else {
                                enc = (byte)(seg << SEG_SHIFT);
                                if (seg < 2) {
                                    enc |= (byte)((sample >> 4) & QUANT_MASK);
                                } else {
                                    enc |= (byte)((sample >> (seg + 3)) & QUANT_MASK);
                                }
                                enc ^= (byte)mask;
                            }
                            /* Now put the encoded sample where it belongs */
                            b[currentPos] = enc;
                            currentPos++;
                        }
                        /* And update pointers and counters for next iteration */
                        readLeft -= readCount;
                        readLen = ((readLeft > tempBufferSize) ? tempBufferSize : readLeft);
                    }

                    if (currentPos == off && readCount < 0) {   // EOF or error
                        return readCount;
                    }

                    return (currentPos - off);  /* Number of bytes written to new buffer */

                } else {

                    int i;
                    int readLen = len / 2;
                    int readOffset = off + len / 2;
                    int readCount = base.Read(b, readOffset, readLen);

                    for (i = off; i < (off + (readCount * 2)); i += 2) {
                        b[i] = tabByte1[b[readOffset] & 0xFF];
                        b[i + 1] = tabByte2[b[readOffset] & 0xFF];
                        readOffset++;
                    }

                    if (readCount <= 0) {       // EOF or error //a@
                        return readCount;
                    }

                    return (i - off);
                }
            }

            public override long skip(long n) {
                // Implementation of this method assumes that we support
                // encoding/decoding from/to 8/16 bits only
                return encode ? base.skip(n * 2) / 2 : base.skip(n / 2) * 2;
            }
        } // end class AlawCodecStream
    } // end class ALAW
}
