/*
 * Copyright (c) 2008, 2021, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.File;
//import java.io.IOException;
//import java.io.OutputStream;
//import java.util.Objects;

//import javax.sound.sampled.AudioFileFormat;
//import javax.sound.sampled.AudioFileFormat.Type;
//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioFormat.Encoding;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.spi.AudioFileWriter;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {
/**
 * Floating-point encoded (format 3) WAVE file writer.
 * 
 * @author Karl Helgason
 */
    public sealed class WaveFloatFileWriter : AudioFileWriter {

        public override AudioFileFormat.Type[] getAudioFileTypes() {
            return new AudioFileFormat.Type[] { AudioFileFormat.Type.WAVE };
        }

        public override AudioFileFormat.Type[] getAudioFileTypes(AudioInputStream stream) {

            if (!stream.getFormat().getEncoding().Equals(AudioFormat.Encoding.PCM_FLOAT))
                return new AudioFileFormat.Type[0];
            return new AudioFileFormat.Type[] { AudioFileFormat.Type.WAVE };
        }

        private void checkFormat(AudioFileFormat.Type type, AudioInputStream stream) {
            if (!AudioFileFormat.Type.WAVE.Equals(type))
                throw new ArgumentException("File type " + type
                        + " not supported.");
            if (!stream.getFormat().getEncoding().Equals(AudioFormat.Encoding.PCM_FLOAT))
                throw new ArgumentException("File format "
                        + stream.getFormat() + " not supported.");
        }

        public void write(AudioInputStream stream, RIFFWriter writer) {
            using (RIFFWriter fmt_chunk = writer.writeChunk("fmt ")) {
                AudioFormat format = stream.getFormat();
                fmt_chunk.writeUnsignedShort(3); // WAVE_FORMAT_IEEE_FLOAT
                fmt_chunk.writeUnsignedShort(format.getChannels());
                fmt_chunk.writeUnsignedInt((int)format.getSampleRate());
                fmt_chunk.writeUnsignedInt(((int)format.getFrameRate())
                        * format.getFrameSize());
                fmt_chunk.writeUnsignedShort(format.getFrameSize());
                fmt_chunk.writeUnsignedShort(format.getSampleSizeInBits());
            }
            using (RIFFWriter data_chunk = writer.writeChunk("data")) {
                stream.transferTo(data_chunk);
            }
        }

        private sealed class NoCloseOutputStream : Stream {
            readonly Stream output;

            internal NoCloseOutputStream(Stream output) {
                this.output = output;
            }

            public override void WriteByte(byte b) {
                output.WriteByte(b);
            }

            public override void Flush() {
                output.Flush();
            }

            public override void Write(byte[] b, int off, int len) {
                output.Write(b, off, len);
            }

            public void write(byte[] b) {
                output.Write(b, 0, b.Length);
            }

            public override bool CanRead {
                get { return false; }
            }

            public override bool CanSeek {
                get { return false; }
            }

            public override bool CanWrite {
                get { return true; }
            }

            public override void Close() {
                //output.Close();
            }

            public override long Length {
                get { return output.Length; }
            }

            public override long Position {
                get {
                    return output.Position;
                }
                set {
                    throw new NotImplementedException();
                }
            }

            public override int Read(byte[] buffer, int offset, int count) {
                throw new NotSupportedException();
            }

            public override int ReadByte() {
                throw new NotSupportedException();
            }

            public override long Seek(long offset, SeekOrigin origin) {
                throw new NotSupportedException();
            }

            public override void SetLength(long value) {
                throw new NotSupportedException();
            }
        }

        private AudioInputStream toLittleEndian(AudioInputStream ais) {
            AudioFormat format = ais.getFormat();
            AudioFormat targetFormat = new AudioFormat(format.getEncoding(), format
                    .getSampleRate(), format.getSampleSizeInBits(), format
                    .getChannels(), format.getFrameSize(), format.getFrameRate(),
                    false);
            return AudioSystem.getAudioInputStream(targetFormat, ais);
        }

        public override int write(AudioInputStream stream, AudioFileFormat.Type fileType, Stream output) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (fileType == null)
                throw new ArgumentNullException(nameof(fileType));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            checkFormat(fileType, stream);
            if (stream.getFormat().isBigEndian())
                stream = toLittleEndian(stream);
            using (RIFFWriter writer = new RIFFWriter(new NoCloseOutputStream(output), "WAVE")) {
                write(stream, writer);
                return (int)writer.getFilePointer();
            }
        }

        public override int write(AudioInputStream stream, AudioFileFormat.Type fileType, FileInfo output) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (fileType == null)
                throw new ArgumentNullException(nameof(fileType));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            checkFormat(fileType, stream);
            if (stream.getFormat().isBigEndian())
                stream = toLittleEndian(stream);
            using (RIFFWriter writer = new RIFFWriter(output, "WAVE")) {
                write(stream, writer);
                return (int)writer.getFilePointer();
            }
        }
    }
}
