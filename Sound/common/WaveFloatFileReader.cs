/*
 * Copyright (c) 2007, 2016, Oracle and/or its affiliates. All rights reserved.
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
//import java.io.InputStream;

//import javax.sound.sampled.AudioFileFormat;
//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioFormat.Encoding;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.UnsupportedAudioFileException;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {
/**
 * Floating-point encoded (format 3) WAVE file loader.
 *
 * @author Karl Helgason
 */
    public sealed class WaveFloatFileReader : SunFileReader {

        internal override StandardFileFormat getAudioFileFormatImpl(Stream stream) {

            RIFFReader riffiterator = new RIFFReader(stream);
            if (!riffiterator.getFormat().Equals("RIFF"))
                throw new UnsupportedAudioFileException();
            if (!riffiterator.getRiffType().Equals("WAVE"))
                throw new UnsupportedAudioFileException();

            bool fmt_found = false;
            bool data_found = false;

            int channels = 1;
            long samplerate = 1;
            int framesize = 1;
            int bits = 1;
            long dataSize = 0;

            while (riffiterator.hasNextChunk()) {
                RIFFReader chunk = riffiterator.nextChunk();

                if (chunk.getFormat().Equals("fmt ")) {
                    fmt_found = true;

                    int format = chunk.readUnsignedShort();
                    if (format != WaveFileFormat.WAVE_FORMAT_IEEE_FLOAT) {
                        throw new UnsupportedAudioFileException();
                    }
                    channels = chunk.readUnsignedShort();
                    samplerate = chunk.readUnsignedInt();
                    /* framerate = */
                    chunk.readUnsignedInt();
                    framesize = chunk.readUnsignedShort();
                    if (framesize == 0) {
                        throw new UnsupportedAudioFileException(
                                "Can not process audio format with 0 frame size");
                    }
                    bits = chunk.readUnsignedShort();
                }
                if (chunk.getFormat().Equals("data")) {
                    dataSize = chunk.getSize();
                    data_found = true;
                    break;
                }
            }

            if (!fmt_found || !data_found) {
                throw new UnsupportedAudioFileException();
            }
            AudioFormat audioformat = new AudioFormat(
                    AudioFormat.Encoding.PCM_FLOAT, samplerate, bits, channels,
                    framesize, samplerate, false);
            return new StandardFileFormat(AudioFileFormat.Type.WAVE, audioformat,
                dataSize / audioformat.getFrameSize());
        }

        public override AudioInputStream getAudioInputStream(Stream stream) {
            StandardFileFormat format = (StandardFileFormat)getAudioFileFormat(stream);
            AudioFormat af = format.getFormat();
            long length = format.getLongFrameLength();
            // we've got everything, the stream is supported and it is at the
            // beginning of the header, so find the data chunk again and return an
            // AudioInputStream
            RIFFReader riffiterator = new RIFFReader(stream);
            while (riffiterator.hasNextChunk()) {
                RIFFReader chunk = riffiterator.nextChunk();
                if (chunk.getFormat().Equals("data")) {
                    return new AudioInputStream(chunk, af, length);
                }
            }
            throw new UnsupportedAudioFileException();
        }
    }
}
