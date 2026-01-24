/*
 * Copyright (c) 1999, 2018, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.BufferedOutputStream;
//import java.io.ByteArrayInputStream;
//import java.io.ByteArrayOutputStream;
//import java.io.DataOutputStream;
//import java.io.File;
//import java.io.FileOutputStream;
//import java.io.IOException;
//import java.io.InputStream;
//import java.io.OutputStream;
//import java.io.RandomAccessFile;
//import java.io.SequenceInputStream;
//import java.util.Objects;

//import javax.sound.sampled.AudioFileFormat;
//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

//$$fb this class is buggy. Should be replaced in future.

/**
 * AIFF file writer.
 *
 * @author Jan Borgersen
 */
    public sealed class AiffFileWriter : SunFileWriter {

        /**
         * Constructs a new AiffFileWriter object.
         */
        public AiffFileWriter()
            : base(new AudioFileFormat.Type[] { AudioFileFormat.Type.AIFF }) {
        }


        // METHODS TO IMPLEMENT AudioFileWriter

        public override AudioFileFormat.Type[] getAudioFileTypes(AudioInputStream stream) {

            AudioFileFormat.Type[] filetypes = new AudioFileFormat.Type[types.Length];
            Array.Copy(types, 0, filetypes, 0, types.Length);

            // make sure we can write this stream
            AudioFormat format = stream.getFormat();
            AudioFormat.Encoding encoding = format.getEncoding();

            if ((AudioFormat.Encoding.ALAW.Equals(encoding)) ||
                (AudioFormat.Encoding.ULAW.Equals(encoding)) ||
                (AudioFormat.Encoding.PCM_SIGNED.Equals(encoding)) ||
                (AudioFormat.Encoding.PCM_UNSIGNED.Equals(encoding))) {

                return filetypes;
            }

            return new AudioFileFormat.Type[0];
        }


        public override int write(AudioInputStream stream, AudioFileFormat.Type fileType, Stream output) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (fileType == null)
                throw new ArgumentNullException(nameof(fileType));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            //$$fb the following check must come first ! Otherwise
            // the next frame length check may throw an IOException and
            // interrupt iterating File Writers. (see bug 4351296)

            // throws IllegalArgumentException if not supported
            AiffFileFormat aiffFileFormat = (AiffFileFormat)getAudioFileFormat(fileType, stream);

            // we must know the total data length to calculate the file length
            if (stream.getFrameLength() == AudioSystem.NOT_SPECIFIED) {
                throw new IOException("stream length not specified");
            }

            return writeAiffFile(stream, aiffFileFormat, output);
        }


        public override int write(AudioInputStream stream, AudioFileFormat.Type fileType, FileInfo output) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (fileType == null)
                throw new ArgumentNullException(nameof(fileType));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            // throws IllegalArgumentException if not supported
            AiffFileFormat aiffFileFormat = (AiffFileFormat)getAudioFileFormat(fileType, stream);

            // first write the file without worrying about length fields
            int bytesWritten;
            using (FileStream fos = new FileStream(output.FullName, FileMode.Create, FileAccess.ReadWrite)) {
                BufferedStream bos = new BufferedStream(fos, bisBufferSize);
                bytesWritten = writeAiffFile(stream, aiffFileFormat, bos);
                bos.Close(); //@?
            }

            // now, if length fields were not specified, calculate them,
            // open as a random access file, write the appropriate fields,
            // close again....
            if (aiffFileFormat.getByteLength() == AudioSystem.NOT_SPECIFIED) {

                // $$kk: 10.22.99: jan: please either implement this or throw an exception!
                // $$fb: 2001-07-13: done. Fixes Bug 4479981
                int channels = aiffFileFormat.getFormat().getChannels();
                int sampleSize = aiffFileFormat.getFormat().getSampleSizeInBits();
                int ssndBlockSize = channels * ((sampleSize + 7) / 8);

                int aiffLength = bytesWritten;
                int ssndChunkSize = aiffLength - aiffFileFormat.getHeaderSize() + 16;
                long dataSize = ssndChunkSize - 16;
                //TODO possibly incorrect round
                int numFrames = (int)(dataSize / ssndBlockSize);

                using (FileStream fs = new FileStream(output.FullName, FileMode.Open, FileAccess.ReadWrite)) {
                    //RandomAccessFile raf = new RandomAccessFile(output, "rw");
                    BigEndianBinaryWriter raf = new BigEndianBinaryWriter(fs);
                    // skip FORM magic
                    raf.BaseStream.Position += (4);
                    raf.Write(aiffLength - 8);
                    // skip aiff2 magic, fver chunk, comm magic, comm size, channel count,
                    raf.BaseStream.Position += (4 + aiffFileFormat.getFverChunkSize() + 4 + 4 + 2);
                    // write frame count
                    raf.Write(numFrames);
                    // skip sample size, samplerate, SSND magic
                    raf.BaseStream.Position += (2 + 10 + 4);
                    raf.Write(ssndChunkSize - 8);
                    // that's all
                    raf.Close(); //@?
                }
            }

            return bytesWritten;
        }


        // -----------------------------------------------------------------------

        /**
         * Returns the AudioFileFormat describing the file that will be written from this AudioInputStream.
         * Throws IllegalArgumentException if not supported.
         */
        private AudioFileFormat getAudioFileFormat(AudioFileFormat.Type type, AudioInputStream stream) {
            if (!isFileTypeSupported(type, stream)) {
                throw new ArgumentException("File type " + type + " not supported.");
            }

            AudioFormat format = null;
            AiffFileFormat fileFormat = null;
            AudioFormat.Encoding encoding = AudioFormat.Encoding.PCM_SIGNED;

            AudioFormat streamFormat = stream.getFormat();
            AudioFormat.Encoding streamEncoding = streamFormat.getEncoding();


            int sampleSizeInBits;
            int fileSize;
            bool convert8to16 = false;

            if ((AudioFormat.Encoding.ALAW.Equals(streamEncoding)) ||
                (AudioFormat.Encoding.ULAW.Equals(streamEncoding))) {

                if (streamFormat.getSampleSizeInBits() == 8) {

                    encoding = AudioFormat.Encoding.PCM_SIGNED;
                    sampleSizeInBits = 16;
                    convert8to16 = true;

                } else {

                    // can't convert non-8-bit ALAW,ULAW
                    throw new ArgumentException("Encoding " + streamEncoding + " supported only for 8-bit data.");
                }
            } else if (streamFormat.getSampleSizeInBits() == 8) {

                encoding = AudioFormat.Encoding.PCM_UNSIGNED;
                sampleSizeInBits = 8;

            } else {

                encoding = AudioFormat.Encoding.PCM_SIGNED;
                sampleSizeInBits = streamFormat.getSampleSizeInBits();
            }


            format = new AudioFormat(encoding,
                                     streamFormat.getSampleRate(),
                                     sampleSizeInBits,
                                     streamFormat.getChannels(),
                                     streamFormat.getFrameSize(),
                                     streamFormat.getFrameRate(),
                                     true); // AIFF is big endian


            if (stream.getFrameLength() != AudioSystem.NOT_SPECIFIED) {
                if (convert8to16) {
                    fileSize = (int)stream.getFrameLength() * streamFormat.getFrameSize() * 2 + AiffFileFormat.AIFF_HEADERSIZE;
                } else {
                    fileSize = (int)stream.getFrameLength() * streamFormat.getFrameSize() + AiffFileFormat.AIFF_HEADERSIZE;
                }
            } else {
                fileSize = AudioSystem.NOT_SPECIFIED;
            }

            fileFormat = new AiffFileFormat(AudioFileFormat.Type.AIFF,
                             fileSize,
                             format,
                             (int)stream.getFrameLength());

            return fileFormat;
        }


        private int writeAiffFile(Stream input, AiffFileFormat aiffFileFormat, Stream output) {

            int bytesRead = 0;
            int bytesWritten = 0;
            Stream fileStream = getFileStream(aiffFileFormat, input);
            byte[] buffer = new byte[bisBufferSize];
            int maxLength = aiffFileFormat.getByteLength();

            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0) { //a@
                if (maxLength > 0) {
                    if (bytesRead < maxLength) {
                        output.Write(buffer, 0, bytesRead);
                        bytesWritten += bytesRead;
                        maxLength -= bytesRead;
                    } else {
                        output.Write(buffer, 0, maxLength);
                        bytesWritten += maxLength;
                        maxLength = 0;
                        break;
                    }

                } else {
                    output.Write(buffer, 0, bytesRead);
                    bytesWritten += bytesRead;
                }
            }

            return bytesWritten;
        }

        private Stream getFileStream(AiffFileFormat aiffFileFormat, Stream audioStream) {

            // private method ... assumes aiffFileFormat is a supported file format

            AudioFormat format = aiffFileFormat.getFormat();
            AudioFormat streamFormat = null;
            AudioFormat.Encoding encoding = null;

            //$$fb a little bit nicer handling of constants

            int headerSize = aiffFileFormat.getHeaderSize();
            //int fverChunkSize       = 0;
            int fverChunkSize = aiffFileFormat.getFverChunkSize();
            int commChunkSize = aiffFileFormat.getCommChunkSize();
            int aiffLength = -1;
            int ssndChunkSize = -1;
            int ssndOffset = aiffFileFormat.getSsndChunkOffset();
            short channels = (short)format.getChannels();
            short sampleSize = (short)format.getSampleSizeInBits();
            int ssndBlockSize = channels * ((sampleSize + 7) / 8);
            int numFrames = aiffFileFormat.getFrameLength();
            long dataSize = -1;
            if (numFrames != AudioSystem.NOT_SPECIFIED) {
                dataSize = (long)numFrames * ssndBlockSize;
                ssndChunkSize = (int)dataSize + 16;
                aiffLength = (int)dataSize + headerSize;
            }
            float sampleFramesPerSecond = format.getSampleRate();
#pragma warning disable 0219
            int compCode = AiffFileFormat.AIFC_PCM;
            byte[] header = null;
#pragma warning restore 0219
            Stream codedAudioStream = audioStream;

            // if we need to do any format conversion, do it here....

            if (audioStream is AudioInputStream) {

                streamFormat = ((AudioInputStream)audioStream).getFormat();
                encoding = streamFormat.getEncoding();


                // $$jb: Note that AIFF samples are ALWAYS signed
                if ((AudioFormat.Encoding.PCM_UNSIGNED.Equals(encoding)) ||
                ((AudioFormat.Encoding.PCM_SIGNED.Equals(encoding)) && !streamFormat.isBigEndian())) {

                    // plug in the transcoder to convert to PCM_SIGNED. big endian
                    codedAudioStream = AudioSystem.getAudioInputStream(new AudioFormat(
                                                         AudioFormat.Encoding.PCM_SIGNED,
                                                         streamFormat.getSampleRate(),
                                                         streamFormat.getSampleSizeInBits(),
                                                         streamFormat.getChannels(),
                                                         streamFormat.getFrameSize(),
                                                         streamFormat.getFrameRate(),
                                                         true),
                                                         (AudioInputStream)audioStream);

                } else if ((AudioFormat.Encoding.ULAW.Equals(encoding)) ||
                       (AudioFormat.Encoding.ALAW.Equals(encoding))) {

                    if (streamFormat.getSampleSizeInBits() != 8) {
                        throw new ArgumentException("unsupported encoding");
                    }

                    //$$fb 2001-07-13: this is probably not what we want:
                    //     writing PCM when ULAW/ALAW is requested. AIFC is able to write ULAW !

                    // plug in the transcoder to convert to PCM_SIGNED_BIG_ENDIAN
                    codedAudioStream = AudioSystem.getAudioInputStream(new AudioFormat(
                                                         AudioFormat.Encoding.PCM_SIGNED,
                                                         streamFormat.getSampleRate(),
                                                         streamFormat.getSampleSizeInBits() * 2,
                                                         streamFormat.getChannels(),
                                                         streamFormat.getFrameSize() * 2,
                                                         streamFormat.getFrameRate(),
                                                         true),
                                                         (AudioInputStream)audioStream);
                }
            }

            // Now create an AIFF stream header...
            using (MemoryStream baos = new MemoryStream()) {
                BigEndianBinaryWriter dos = new BigEndianBinaryWriter(baos);

                // Write the outer FORM chunk
                dos.Write(AiffFileFormat.AIFF_MAGIC);
                dos.Write((aiffLength - 8));
                dos.Write(AiffFileFormat.AIFF_MAGIC2);

                // Write a FVER chunk - only for AIFC
                //dos.writeInt(FVER_MAGIC);
                //dos.writeInt( (fverChunkSize-8) );
                //dos.writeInt(FVER_TIMESTAMP);

                // Write a COMM chunk
                dos.Write(AiffFileFormat.COMM_MAGIC);
                dos.Write((commChunkSize - 8));
                dos.Write(channels);
                dos.Write(numFrames);
                dos.Write(sampleSize);
                write_ieee_extended(dos, sampleFramesPerSecond);   // 10 bytes

                //Only for AIFC
                //dos.writeInt(compCode);
                //dos.writeInt(compCode);
                //dos.writeShort(0);

                // Write the SSND chunk header
                dos.Write(AiffFileFormat.SSND_MAGIC);
                dos.Write((ssndChunkSize - 8));
                // ssndOffset and ssndBlockSize set to 0 upon
                // recommendation in "Sound Manager" chapter in
                // "Inside Macintosh Sound", pp 2-87  (from Babu)
                dos.Write(0);   // ssndOffset
                dos.Write(0);   // ssndBlockSize
                header = baos.ToArray();
            }
            return CreateConcatStream(header, codedAudioStream);
        }

        // HELPER METHODS

        private const int DOUBLE_MANTISSA_LENGTH = 52;
        private const int DOUBLE_EXPONENT_LENGTH = 11;
        private const ulong DOUBLE_SIGN_MASK = 0x8000000000000000L;
        private const long DOUBLE_EXPONENT_MASK = 0x7FF0000000000000L;
        private const long DOUBLE_MANTISSA_MASK = 0x000FFFFFFFFFFFFFL;
        private const int DOUBLE_EXPONENT_OFFSET = 1023;

        private const int EXTENDED_EXPONENT_OFFSET = 16383;
        private const int EXTENDED_MANTISSA_LENGTH = 63;
        private const int EXTENDED_EXPONENT_LENGTH = 15;
        private const ulong EXTENDED_INTEGER_MASK = 0x8000000000000000L;

        /**
         * Extended precision IEEE floating-point conversion routine.
         * @argument DataOutputStream
         * @argument double
         * @exception IOException
         */
        private void write_ieee_extended(BigEndianBinaryWriter dos, float f) {
            /* The special cases NaN, Infinity and Zero are ignored, since
               they do not represent useful sample rates anyway.
               Denormalized number aren't handled, too. Below, there is a cast
               from float to double. We hope that in this conversion,
               numbers are normalized. Numbers that cannot be normalized are
               ignored, too, as they, too, do not represent useful sample rates. */
            ulong doubleBits = (ulong)BitConverter.DoubleToInt64Bits((double)f); // Double.doubleToLongBits((double)f);

            ulong sign = (doubleBits & DOUBLE_SIGN_MASK)
                >> (DOUBLE_EXPONENT_LENGTH + DOUBLE_MANTISSA_LENGTH);
            ulong doubleExponent = (doubleBits & DOUBLE_EXPONENT_MASK)
                >> DOUBLE_MANTISSA_LENGTH;
            ulong doubleMantissa = doubleBits & DOUBLE_MANTISSA_MASK;

            ulong extendedExponent = (ulong)(doubleExponent - DOUBLE_EXPONENT_OFFSET
                + EXTENDED_EXPONENT_OFFSET);
            ulong extendedMantissa = (ulong)(doubleMantissa
                << (EXTENDED_MANTISSA_LENGTH - DOUBLE_MANTISSA_LENGTH));
            ulong extendedSign = sign << EXTENDED_EXPONENT_LENGTH;
            short extendedBits79To64 = (short)(extendedSign | extendedExponent);
            ulong extendedBits63To0 = EXTENDED_INTEGER_MASK | extendedMantissa;

            dos.Write(extendedBits79To64);
            dos.Write(extendedBits63To0);
        }
    }
}
