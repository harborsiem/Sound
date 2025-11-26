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

//import java.io.File;
//import java.io.IOException;
//import java.io.InputStream;

//import javax.sound.midi.InvalidMidiDataException;
//import javax.sound.midi.MetaMessage;
//import javax.sound.midi.MidiEvent;
//import javax.sound.midi.MidiMessage;
//import javax.sound.midi.MidiSystem;
//import javax.sound.midi.MidiUnavailableException;
//import javax.sound.midi.Receiver;
//import javax.sound.midi.Sequence;
//import javax.sound.midi.Track;

//import javax.sound.sampled.AudioFileFormat.Type;
//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.UnsupportedAudioFileException;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Midi;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * MIDI File Audio Renderer/Reader.
 *
 * @author Karl Helgason
 */
    public sealed class SoftMidiAudioFileReader : SunFileReader {

        private static readonly AudioFileFormat.Type MIDI = new AudioFileFormat.Type("MIDI", "mid");
        private static AudioFormat format = new AudioFormat(44100, 16, 2, true, false);

        private static StandardFileFormat getAudioFileFormat(Sequence seq) {
            long totallen = seq.getMicrosecondLength() / 1000000;
            long len = (long)(format.getFrameRate() * (totallen + 4));
            return new StandardFileFormat(MIDI, format, (int)len);
        }

        private AudioInputStream getAudioInputStream(Sequence seq) {
            IAudioSynthesizer synth = (IAudioSynthesizer)new SoftSynthesizer();
            AudioInputStream stream;
            IReceiver recv;
            try {
                stream = synth.openStream(format, null);
                recv = synth.getReceiver();
            } catch (MidiUnavailableException e) {
                throw new InvalidMidiDataException(e.ToString());
            }
            float divtype = seq.getDivisionType();
            Track[] tracks = seq.getTracks();
            int[] trackspos = new int[tracks.Length];
            int mpq = 500000;
            int seqres = seq.getResolution();
            long lasttick = 0;
            long curtime = 0;
            while (true) {
                MidiEvent selevent = null;
                int seltrack = -1;
                for (int i = 0; i < tracks.Length; i++) {
                    int trackpos = trackspos[i];
                    Track track = tracks[i];
                    if (trackpos < track.size()) {
                        MidiEvent evnt = track.get(trackpos);
                        if (selevent == null || evnt.getTick() < selevent.getTick()) {
                            selevent = evnt;
                            seltrack = i;
                        }
                    }
                }
                if (seltrack == -1)
                    break;
                trackspos[seltrack]++;
                long tick = selevent.getTick();
                if (divtype == Sequence.PPQ)
                    curtime += ((tick - lasttick) * mpq) / seqres;
                else
                    curtime = (long)((tick * 1000000.0 * divtype) / seqres);
                lasttick = tick;
                MidiMessage msg = selevent.getMessage();
                if (msg is MetaMessage) {
                    if (divtype == Sequence.PPQ) {
                        if (((MetaMessage)msg).getType() == 0x51) {
                            byte[] data = ((MetaMessage)msg).getData();
                            if (data.Length < 3) {
                                throw new InvalidMidiDataException();
                            }
                            mpq = ((data[0] & 0xff) << 16)
                                    | ((data[1] & 0xff) << 8) | (data[2] & 0xff);
                        }
                    }
                } else {
                    recv.send(msg, curtime);
                }
            }

            long totallen = curtime / 1000000;
            long len = (long)(stream.getFormat().getFrameRate() * (totallen + 4));
            stream = new AudioInputStream(stream, stream.getFormat(), len);
            return stream;
        }

        public override AudioInputStream getAudioInputStream(Stream stream) {
            InputStream iStream = stream as InputStream;
            if (stream != null && iStream == null) {
                iStream = new InputStreamImpl(stream);
            }
            iStream.mark(200);
            try {
                return getAudioInputStream(MidiSystem.getSequence(stream));
            }
            // stream is unsupported or the header is less than was expected
            catch (InvalidMidiDataException) {
                iStream.reset();
                throw new UnsupportedAudioFileException();
            } catch (IOException) {
                iStream.reset();
                throw new UnsupportedAudioFileException();
            }
        }

        internal override StandardFileFormat getAudioFileFormatImpl(Stream stream) {
            try {
                return getAudioFileFormat(MidiSystem.getSequence(stream));
            } catch (InvalidMidiDataException) {
                throw new UnsupportedAudioFileException();
            }
        }
    }
}
