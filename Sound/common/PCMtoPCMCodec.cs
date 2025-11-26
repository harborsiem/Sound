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
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {
/**
 * Converts among signed/unsigned and little/big endianness of sampled.
 *
 * @author Jan Borgersen
 */
    public sealed class PCMtoPCMCodec : FormatConversionProvider {

        public override AudioFormat.Encoding[] getSourceEncodings() {
            return new AudioFormat.Encoding[] { AudioFormat.Encoding.PCM_SIGNED, AudioFormat.Encoding.PCM_UNSIGNED };
        }

        public override AudioFormat.Encoding[] getTargetEncodings() {
            return getSourceEncodings();
        }

        public override AudioFormat.Encoding[] getTargetEncodings(AudioFormat sourceFormat) {

            int sampleSize = sourceFormat.getSampleSizeInBits();
            AudioFormat.Encoding encoding = sourceFormat.getEncoding();
            if (sampleSize == 8) {
                if (encoding.Equals(AudioFormat.Encoding.PCM_SIGNED)) {
                    return new AudioFormat.Encoding[]{
                        AudioFormat.Encoding.PCM_UNSIGNED
                };
                }
                if (encoding.Equals(AudioFormat.Encoding.PCM_UNSIGNED)) {
                    return new AudioFormat.Encoding[]{
                        AudioFormat.Encoding.PCM_SIGNED
                };
                }
            } else if (sampleSize == 16) {
                if (encoding.Equals(AudioFormat.Encoding.PCM_SIGNED)
                        || encoding.Equals(AudioFormat.Encoding.PCM_UNSIGNED)) {
                    return new AudioFormat.Encoding[]{
                        AudioFormat.Encoding.PCM_UNSIGNED,
                        AudioFormat.Encoding.PCM_SIGNED
                };
                }
            }
            return new AudioFormat.Encoding[0];
        }

        public override AudioFormat[] getTargetFormats(AudioFormat.Encoding targetEncoding, AudioFormat sourceFormat) {
            if (targetEncoding == null)
                throw new ArgumentNullException(nameof(targetEncoding));

            // filter out targetEncoding from the old getOutputFormats( sourceFormat ) method

            AudioFormat[] formats = getOutputFormats(sourceFormat);
            List<AudioFormat> newFormats = new List<AudioFormat>();
            for (int i = 0; i < formats.Length; i++) {
                if (formats[i].getEncoding().Equals(targetEncoding)) {
                    newFormats.Add(formats[i]);
                }
            }

            AudioFormat[] formatArray = new AudioFormat[newFormats.Count];

            for (int i = 0; i < formatArray.Length; i++) {
                formatArray[i] = newFormats[i];
            }

            return formatArray;
        }


        public override AudioInputStream getAudioInputStream(AudioFormat.Encoding targetEncoding, AudioInputStream sourceStream) {

            if (isConversionSupported(targetEncoding, sourceStream.getFormat())) {

                AudioFormat sourceFormat = sourceStream.getFormat();
                AudioFormat targetFormat = new AudioFormat(targetEncoding,
                                    sourceFormat.getSampleRate(),
                                    sourceFormat.getSampleSizeInBits(),
                                    sourceFormat.getChannels(),
                                    sourceFormat.getFrameSize(),
                                    sourceFormat.getFrameRate(),
                                    sourceFormat.isBigEndian());

                return getConvertedStream(targetFormat, sourceStream);

            } else {
                throw new ArgumentException("Unsupported conversion: " + sourceStream.getFormat().ToString() + " to " + targetEncoding.ToString());
            }
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

                cs = new PCMtoPCMCodecStream(this, stream, outputFormat);
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

            int sampleSize = inputFormat.getSampleSizeInBits();
            bool isBigEndian = inputFormat.isBigEndian();


            if (sampleSize == 8) {
                if (AudioFormat.Encoding.PCM_SIGNED.Equals(inputFormat.getEncoding())) {

                    format = new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 false);
                    formats.Add(format);
                }

                if (AudioFormat.Encoding.PCM_UNSIGNED.Equals(inputFormat.getEncoding())) {

                    format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 false);
                    formats.Add(format);
                }

            } else if (sampleSize == 16) {

                if (AudioFormat.Encoding.PCM_SIGNED.Equals(inputFormat.getEncoding()) && isBigEndian) {

                    format = new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 true);
                    formats.Add(format);
                    format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 false);
                    formats.Add(format);
                    format = new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 false);
                    formats.Add(format);
                }

                if (AudioFormat.Encoding.PCM_UNSIGNED.Equals(inputFormat.getEncoding()) && isBigEndian) {

                    format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 true);
                    formats.Add(format);
                    format = new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 false);
                    formats.Add(format);
                    format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 false);
                    formats.Add(format);
                }

                if (AudioFormat.Encoding.PCM_SIGNED.Equals(inputFormat.getEncoding()) && !isBigEndian) {

                    format = new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 false);
                    formats.Add(format);
                    format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 true);
                    formats.Add(format);
                    format = new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 true);
                    formats.Add(format);
                }

                if (AudioFormat.Encoding.PCM_UNSIGNED.Equals(inputFormat.getEncoding()) && !isBigEndian) {

                    format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 false);
                    formats.Add(format);
                    format = new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 true);
                    formats.Add(format);
                    format = new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                                 inputFormat.getSampleRate(),
                                 inputFormat.getSampleSizeInBits(),
                                 inputFormat.getChannels(),
                                 inputFormat.getFrameSize(),
                                 inputFormat.getFrameRate(),
                                 true);
                    formats.Add(format);
                }
            }
            AudioFormat[] formatArray;

            lock (formats) {

                formatArray = new AudioFormat[formats.Count];

                for (int i = 0; i < formatArray.Length; i++) {

                    formatArray[i] = formats[i];
                }
            }

            return formatArray;
        }

        internal class PCMtoPCMCodecStream : AudioInputStream {

            private const int PCM_SWITCH_SIGNED_8BIT = 1;
            private const int PCM_SWITCH_ENDIAN = 2;
            private const int PCM_SWITCH_SIGNED_LE = 3;
            private const int PCM_SWITCH_SIGNED_BE = 4;
            private const int PCM_UNSIGNED_LE2SIGNED_BE = 5;
            private const int PCM_SIGNED_LE2UNSIGNED_BE = 6;
            private const int PCM_UNSIGNED_BE2SIGNED_LE = 7;
            private const int PCM_SIGNED_BE2UNSIGNED_LE = 8;

            private readonly int sampleSizeInBytes;
            private int conversionType = 0;

            private PCMtoPCMCodec caller;

            internal PCMtoPCMCodecStream(PCMtoPCMCodec caller, AudioInputStream stream, AudioFormat outputFormat)

                : base(stream, outputFormat, -1) {
                this.caller = caller;
                int sampleSizeInBits = 0;
                AudioFormat.Encoding inputEncoding = null;
                AudioFormat.Encoding outputEncoding = null;
                bool inputIsBigEndian;
                bool outputIsBigEndian;

                AudioFormat inputFormat = stream.getFormat();

                // throw an IllegalArgumentException if not ok
                if (!(caller.isConversionSupported(inputFormat, outputFormat))) {

                    throw new ArgumentException("Unsupported conversion: " + inputFormat.ToString() + " to " + outputFormat.ToString());
                }

                inputEncoding = inputFormat.getEncoding();
                outputEncoding = outputFormat.getEncoding();
                inputIsBigEndian = inputFormat.isBigEndian();
                outputIsBigEndian = outputFormat.isBigEndian();
                sampleSizeInBits = inputFormat.getSampleSizeInBits();
                sampleSizeInBytes = sampleSizeInBits / 8;

                // determine conversion to perform

                if (sampleSizeInBits == 8) {
                    if (AudioFormat.Encoding.PCM_UNSIGNED.Equals(inputEncoding) &&
                        AudioFormat.Encoding.PCM_SIGNED.Equals(outputEncoding)) {
                        conversionType = PCM_SWITCH_SIGNED_8BIT;

                    } else if (AudioFormat.Encoding.PCM_SIGNED.Equals(inputEncoding) &&
                           AudioFormat.Encoding.PCM_UNSIGNED.Equals(outputEncoding)) {
                        conversionType = PCM_SWITCH_SIGNED_8BIT;
                    }
                } else {

                    if (inputEncoding.Equals(outputEncoding) && (inputIsBigEndian != outputIsBigEndian)) {

                        conversionType = PCM_SWITCH_ENDIAN;


                    } else if (AudioFormat.Encoding.PCM_UNSIGNED.Equals(inputEncoding) && !inputIsBigEndian &&
                            AudioFormat.Encoding.PCM_SIGNED.Equals(outputEncoding) && outputIsBigEndian) {

                        conversionType = PCM_UNSIGNED_LE2SIGNED_BE;

                    } else if (AudioFormat.Encoding.PCM_SIGNED.Equals(inputEncoding) && !inputIsBigEndian &&
                           AudioFormat.Encoding.PCM_UNSIGNED.Equals(outputEncoding) && outputIsBigEndian) {

                        conversionType = PCM_SIGNED_LE2UNSIGNED_BE;

                    } else if (AudioFormat.Encoding.PCM_UNSIGNED.Equals(inputEncoding) && inputIsBigEndian &&
                           AudioFormat.Encoding.PCM_SIGNED.Equals(outputEncoding) && !outputIsBigEndian) {

                        conversionType = PCM_UNSIGNED_BE2SIGNED_LE;

                    } else if (AudioFormat.Encoding.PCM_SIGNED.Equals(inputEncoding) && inputIsBigEndian &&
                           AudioFormat.Encoding.PCM_UNSIGNED.Equals(outputEncoding) && !outputIsBigEndian) {

                        conversionType = PCM_SIGNED_BE2UNSIGNED_LE;
                    }
                }

                // set the audio stream length in frames if we know it

                frameSize = inputFormat.getFrameSize();
                if (frameSize == AudioSystem.NOT_SPECIFIED) {
                    frameSize = 1;
                }
                if (stream is AudioInputStream) {
                    frameLength = stream.getFrameLength();
                } else {
                    frameLength = AudioSystem.NOT_SPECIFIED;
                }

                // set framePos to zero
                framePos = 0;
            }

            /**
             * Note that this only works for sign conversions.
             * Other conversions require a read of at least 2 bytes.
             */

            public override int ReadByte() {

                // $$jb: do we want to implement this function?

                int temp;
                //byte tempbyte;

                if (frameSize == 1) {
                    if (conversionType == PCM_SWITCH_SIGNED_8BIT) {
                        temp = base.ReadByte();

                        if (temp < 0) return temp;      // EOF or error

                        return (byte)(temp + 0x80);
                        //a@ bug in Java
                        //tempbyte = (byte)(temp & 0xf);
                        //tempbyte = (tempbyte >= 0) ? (byte)(0x80 | tempbyte) : (byte)(0x7F & tempbyte);
                        //temp = (int)tempbyte & 0xf;

                        //return temp;

                    } else {
                        // $$jb: what to return here?
                        throw new IOException("cannot read a single byte if frame size > 1");
                    }
                } else {
                    throw new IOException("cannot read a single byte if frame size > 1");
                }
            }

            //public override int read(byte[] b) {

            //    return Read(b, 0, b.Length);
            //}

            public override int Read(byte[] b, int off, int len) {

#pragma warning disable 0168
                int i;
#pragma warning restore 0168

                // don't read fractional frames
                if (len % frameSize != 0) {
                    len -= (len % frameSize);
                }
                // don't read past our own set length
                if ((frameLength != AudioSystem.NOT_SPECIFIED) && ((len / frameSize) > (frameLength - framePos))) {
                    len = (int)(frameLength - framePos) * frameSize;
                }

                int readCount = base.Read(b, off, len);
#pragma warning disable 0168
                byte tempByte;
#pragma warning restore 0168

                if (readCount <= 0) {   // EOF or error //a@
                    return readCount;
                }

                // now do the conversions

                switch (conversionType) {

                    case PCM_SWITCH_SIGNED_8BIT:
                        switchSigned8bit(b, off, len, readCount);
                        break;

                    case PCM_SWITCH_ENDIAN:
                        switchEndian(b, off, len, readCount);
                        break;

                    case PCM_SWITCH_SIGNED_LE:
                        switchSignedLE(b, off, len, readCount);
                        break;

                    case PCM_SWITCH_SIGNED_BE:
                        switchSignedBE(b, off, len, readCount);
                        break;

                    case PCM_UNSIGNED_LE2SIGNED_BE:
                    case PCM_SIGNED_LE2UNSIGNED_BE:
                        switchSignedLE(b, off, len, readCount);
                        switchEndian(b, off, len, readCount);
                        break;

                    case PCM_UNSIGNED_BE2SIGNED_LE:
                    case PCM_SIGNED_BE2UNSIGNED_LE:
                        switchSignedBE(b, off, len, readCount);
                        switchEndian(b, off, len, readCount);
                        break;

                    default: break;
                        // do nothing
                }

                // we've done the conversion, just return the readCount
                return readCount;
            }

            private void switchSigned8bit(byte[] b, int off, int len, int readCount) {

                for (int i = off; i < (off + readCount); i++) {
                    unchecked {
                        b[i] += 0x80;
                    }
                    //b[i] = ((sbyte)b[i] >= 0) ? (byte)(0x80 | b[i]) : (byte)(0x7F & b[i]);
                }
            }

            private void switchSignedBE(byte[] b, int off, int len, int readCount) {

                for (int i = off; i < (off + readCount); i += sampleSizeInBytes) {
                    unchecked {
                        b[i] += 0x80;
                    }
                    //b[i] = ((sbyte)b[i] >= 0) ? (byte)(0x80 | b[i]) : (byte)(0x7F & b[i]);
                }
            }

            private void switchSignedLE(byte[] b, int off, int len, int readCount) {

                for (int i = (off + sampleSizeInBytes - 1); i < (off + readCount); i += sampleSizeInBytes) {
                    unchecked {
                        b[i] += 0x80;
                    }
                    //b[i] = ((sbyte)b[i] >= 0) ? (byte)(0x80 | b[i]) : (byte)(0x7F & b[i]);
                }
            }

            private void switchEndian(byte[] b, int off, int len, int readCount) {

                if (sampleSizeInBytes == 2) {
                    for (int i = off; i < (off + readCount); i += sampleSizeInBytes) {
                        byte temp;
                        temp = b[i];
                        b[i] = b[i + 1];
                        b[i + 1] = temp;
                    }
                }
            }
        } // end class PCMtoPCMCodecStream
    } // end class PCMtoPCMCodec
}
