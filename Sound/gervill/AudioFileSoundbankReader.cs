/*
 @@@ Need work in line 111
 * Copyright (c) 2007, 2025, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.ByteArrayOutputStream;
//import java.io.DataInputStream;
//import java.io.File;
//import java.io.IOException;
//import java.io.InputStream;
//import java.net.URL;

//import javax.sound.midi.InvalidMidiDataException;
//import javax.sound.midi.Soundbank;
//import javax.sound.midi.spi.SoundbankReader;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.UnsupportedAudioFileException;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Midi;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

    /**
     * Soundbank reader that uses audio files as soundbanks.
     * 
     * @author Karl Helgason
     */
    public sealed class AudioFileSoundbankReader : SoundbankReader {

        public override ISoundbank getSoundbank(Uri url) {
            try {
                using (AudioInputStream ais = AudioSystem.getAudioInputStream(url)) {
                    ISoundbank sbk = getSoundbank(ais);
                    return sbk;
                }
            } catch (Exception ex) {
                if (ex is UnsupportedAudioFileException || ex is IOException) {
                    return null;
                } else
                    throw;
            }
        }

        public override ISoundbank getSoundbank(Stream stream) {
            InputStream iStream = stream as InputStream;
            if (stream != null && iStream == null) {
                iStream = new InputStreamImpl(stream);
            }
            iStream.mark(512);
            try {
                AudioInputStream ais = AudioSystem.getAudioInputStream(stream);
                ISoundbank sbk = getSoundbank(ais);
                if (sbk != null)
                    return sbk;
            } catch (Exception ex) {
                if (ex is UnsupportedAudioFileException || ex is IOException) {
                    
                } else
                    throw;
            }
            iStream.reset();
            return null;
        }

        public ISoundbank getSoundbank(AudioInputStream ais) {
            int MEGABYTE = 1048576;
            int DEFAULT_BUFFER_SIZE = 65536;
            int MAX_FRAME_SIZE = 1024;
            try {
                byte[] buffer;
                int frameSize = ais.getFormat().getFrameSize();
                if (frameSize <= 0 || frameSize > MAX_FRAME_SIZE) {
                    throw new InvalidMidiDataException("Formats with frame size "
                            + frameSize + " are not supported");
                }

                long totalSize = ais.getFrameLength() * frameSize;
                if (totalSize >= Int32.MaxValue - 2) {
                    throw new InvalidMidiDataException(
                            "Can not allocate enough memory to read audio data.");
                }

                //long maximumHeapSize1 =GC.GetTotalMemory(false);
                //long maximumHeapSize = (long)((Runtime.getRuntime().maxMemory() -
                //        (Runtime.getRuntime().totalMemory() - Runtime.getRuntime().freeMemory())) * 0.9);
                //if (totalSize > maximumHeapSize) {
                //    throw new InvalidMidiDataException(
                //            "Insufficient heap size to render audio data.");
                //}

                if (ais.getFrameLength() == -1 || totalSize > MEGABYTE) {
                    MemoryStream baos = new MemoryStream();
                    byte[] buff = new byte[DEFAULT_BUFFER_SIZE - (DEFAULT_BUFFER_SIZE % frameSize)];
                    int ret;
                    while ((ret = ais.Read(buff, 0, buff.Length)) > 0) {
                        baos.Write(buff, 0, ret);
                    }
                    ais.Close();
                    buffer = baos.ToArray();
                } else {
                    buffer = new byte[(int)totalSize];
                    new BinaryReader(ais).Read(buffer, 0, buffer.Length);
                }
                ModelByteBufferWavetable osc = new ModelByteBufferWavetable(
                        new ModelByteBuffer(buffer), ais.getFormat(), -4800);
                ModelPerformer performer = new ModelPerformer();
                performer.getOscillators().Add(osc);

                SimpleSoundbank sbk = new SimpleSoundbank();
                SimpleInstrument ins = new SimpleInstrument();
                ins.Add(performer);
                sbk.addInstrument(ins);
                return sbk;
            } catch (Exception) {
                return null;
            }
        }

        public override ISoundbank getSoundbank(FileInfo file) {
            try {
                AudioInputStream ais = AudioSystem.getAudioInputStream(file);
                ais.Close();
                ModelByteBufferWavetable osc = new ModelByteBufferWavetable(
                        new ModelByteBuffer(file, 0, file.Length), -4800);
                ModelPerformer performer = new ModelPerformer();
                performer.getOscillators().Add(osc);
                SimpleSoundbank sbk = new SimpleSoundbank();
                SimpleInstrument ins = new SimpleInstrument();
                ins.Add(performer);
                sbk.addInstrument(ins);
                return sbk;
            } catch (Exception ex) {
                if (ex is UnsupportedAudioFileException || ex is IOException) {
                    return null;
                } else
                    throw;
            }
        }
    }
}
