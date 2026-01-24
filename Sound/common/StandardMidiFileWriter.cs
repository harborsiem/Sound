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

//import java.io.ByteArrayInputStream;
//import java.io.ByteArrayOutputStream;
//import java.io.DataOutputStream;
//import java.io.File;
//import java.io.FileOutputStream;
//import java.io.IOException;
//import java.io.InputStream;
//import java.io.OutputStream;
//import java.io.PipedInputStream;
//import java.io.PipedOutputStream;
//import java.io.SequenceInputStream;
//import java.util.Objects;

//import javax.sound.midi.InvalidMidiDataException;
//import javax.sound.midi.MetaMessage;
//import javax.sound.midi.MidiEvent;
//import javax.sound.midi.Sequence;
//import javax.sound.midi.ShortMessage;
//import javax.sound.midi.SysexMessage;
//import javax.sound.midi.Track;    
//import javax.sound.midi.spi.MidiFileWriter;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {
/**
 * MIDI file writer.
 *
 * @author Kara Kytle
 * @author Jan Borgersen
 */
    public sealed class StandardMidiFileWriter : MidiFileWriter {

        private const int MThd_MAGIC = 0x4d546864;  // 'MThd'
        private const int MTrk_MAGIC = 0x4d54726b;  // 'MTrk'

        private const int ONE_BYTE = 1;
        private const int TWO_BYTE = 2;
        private const int SYSEX = 3;
        private const int META = 4;
        private const int ERROR = 5;
        private const int IGNORE = 6;

        private const int MIDI_TYPE_0 = 0;
        private const int MIDI_TYPE_1 = 1;

        private BigEndianBinaryWriter tddos;               // data output stream for track writing

        /**
         * MIDI parser types.
         */
        private static readonly int[] types = {
            MIDI_TYPE_0,
            MIDI_TYPE_1
        };

        public override int[] getMidiFileTypes() {
            int[] localArray = new int[types.Length];
            Array.Copy(types, 0, localArray, 0, types.Length);
            return localArray;
        }

        /**
         * Obtains the file types that this provider can write from the
         * sequence specified.
         * @param sequence the sequence for which midi file type support
         * is queried
         * @return array of file types.  If no file types are supported, 
         * returns an array of length 0.
         */
        public override int[] getMidiFileTypes(Sequence sequence) {
            int[] typesArray;
            Track[] tracks = sequence.getTracks();

            if (tracks.Length == 1) {
                typesArray = new int[2];
                typesArray[0] = MIDI_TYPE_0;
                typesArray[1] = MIDI_TYPE_1;
            } else {
                typesArray = new int[1];
                typesArray[0] = MIDI_TYPE_1;
            }

            return typesArray;
        }

        public override int write(Sequence input, int type, Stream output) {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (!isFileTypeSupported(type, input)) {
                throw new ArgumentException("Could not write MIDI file");
            }

            // First get the fileStream from this sequence
            Stream fileStream = getFileStream(type, input);
            if (fileStream == null) {
                throw new ArgumentException("Could not write MIDI file");
            }
            long bytesWritten = fileStream.transferTo(output);
            // Done....return bytesWritten
            return (int)bytesWritten;
        }

        public override int write(Sequence input, int type, FileInfo output) {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            using (FileStream fos = new FileStream(output.FullName, FileMode.Create, FileAccess.ReadWrite)) { // throws IOException
                int bytesWritten = write(input, type, fos);
                return bytesWritten;
            }
        }

        //=================================================================================

        private Stream getFileStream(int type, Sequence sequence) {
            Track[] tracks = sequence.getTracks();
            int bytesBuilt = 0;
            int headerLength = 14;
            int length = 0;
            int timeFormat;
            float divtype;

            BigEndianBinaryWriter hdos = null;
            MemoryStream headerStream = null; //PipedInputStream

            MemoryStream[] trackStreams = null;
            MemoryStream trackStream = null;
            MemoryStream fStream = null;

            // Determine the filetype to write
            if (type == MIDI_TYPE_0) {
                if (tracks.Length != 1) {
                    return null;
                }
            } else if (type == MIDI_TYPE_1) {
                if (tracks.Length < 1) { // $$jb: 05.31.99: we _can_ write TYPE_1 if tracks.length==1
                    return null;
                }
            } else {
                if (tracks.Length == 1) {
                    type = MIDI_TYPE_0;
                } else if (tracks.Length > 1) {
                    type = MIDI_TYPE_1;
                } else {
                    return null;
                }
            }

            // Now build the file one track at a time
            // Note that above we made sure that MIDI_TYPE_0 only happens
            // if tracks.length==1

            trackStreams = new MemoryStream[tracks.Length];
            int trackCount = 0;
            for (int i = 0; i < tracks.Length; i++) {
                try {
                    trackStreams[trackCount] = (MemoryStream)writeTrack(tracks[i], type);
                    trackCount++;
                } catch (InvalidMidiDataException e) {
                    if (Printer.err) Printer.Err("Exception in write: " + e.Message);
                }
                //bytesBuilt += trackStreams[i].getLength();
            }

            // Now sequence the track streams
            if (trackCount == 1) {
                trackStream = trackStreams[0];
            } else if (trackCount > 1) {
                trackStream = trackStreams[0];
                trackStream.Position = trackStream.Length;
                for (int i = 1; i < tracks.Length; i++) {
                    // fix for 5048381: NullPointerException when saving a MIDI sequence
                    // don't include failed track streams
                    if (trackStreams[i] != null) {
                        trackStreams[i].CopyTo(trackStream);
                    }
                }
            } else {
                throw new ArgumentException("invalid MIDI data in sequence");
            }

            // Now build the header...
            headerStream = new MemoryStream(); //PipedInputStream
            hdos = new BigEndianBinaryWriter(headerStream);

            // Write the magic number
            hdos.Write(MThd_MAGIC);

            // Write the header length
            hdos.Write(headerLength - 8);

            // Write the filetype
            if (type == MIDI_TYPE_0) {
                hdos.Write((short)0);
            } else {
                // MIDI_TYPE_1
                hdos.Write((short)1);
            }

            // Write the number of tracks
            hdos.Write((short)trackCount);

            // Determine and write the timing format
            divtype = sequence.getDivisionType();
            if (divtype == Sequence.PPQ) {
                timeFormat = sequence.getResolution();
            } else if (divtype == Sequence.SMPTE_24) {
                timeFormat = (24 << 8) * -1;
                timeFormat += (sequence.getResolution() & 0xFF);
            } else if (divtype == Sequence.SMPTE_25) {
                timeFormat = (25 << 8) * -1;
                timeFormat += (sequence.getResolution() & 0xFF);
            } else if (divtype == Sequence.SMPTE_30DROP) {
                timeFormat = (29 << 8) * -1;
                timeFormat += (sequence.getResolution() & 0xFF);
            } else if (divtype == Sequence.SMPTE_30) {
                timeFormat = (30 << 8) * -1;
                timeFormat += (sequence.getResolution() & 0xFF);
            } else {
                // $$jb: 04.08.99: What to really do here?
                return null;
            }
            hdos.Write((short)timeFormat);

            // now construct an InputStream to become the FileStream
            trackStream.Position = 0;
            fStream = SunFileWriter.CreateConcatStream(headerStream.ToArray(), trackStream);
            hdos.Close();

            length = bytesBuilt + headerLength;
            return fStream;
        }

        /**
         * Returns ONE_BYTE, TWO_BYTE, SYSEX, META, 
         * ERROR, or IGNORE (i.e. invalid for a MIDI file)
         */
        private int getType(int byteValue) {
            if ((byteValue & 0xF0) == 0xF0) {
                switch (byteValue) {
                    case 0xF0:
                    case 0xF7:
                        return SYSEX;
                    case 0xFF:
                        return META;
                }
                return IGNORE;
            }

            switch (byteValue & 0xF0) {
                case 0x80:
                case 0x90:
                case 0xA0:
                case 0xB0:
                case 0xE0:
                    return TWO_BYTE;
                case 0xC0:
                case 0xD0:
                    return ONE_BYTE;
            }
            return ERROR;
        }

        private const long mask = 0x7F;

        private int writeVarInt(long value) {
            int len = 1;
            int shift = 63; // number of bitwise left-shifts of mask
            // first screen out leading zeros
            while ((shift > 0) && ((value & (mask << shift)) == 0)) shift -= 7;
            // then write actual values
            while (shift > 0) {
                tddos.Write((byte)((int)(((value & (mask << shift)) >> shift) | 0x80)));
                shift -= 7;
                len++;
            }
            tddos.Write((byte)((int)(value & mask)));
            return len;
        }

        private Stream writeTrack(Track track, int type) {
            int bytesWritten = 0;
#pragma warning disable 0219
            int lastBytesWritten = 0;
#pragma warning restore 0219
            int size = track.size();
            MemoryStream thpos = new MemoryStream(); // PipedOutputStream();
            BigEndianBinaryWriter thdos = new BigEndianBinaryWriter(thpos);

            MemoryStream tdbos = new MemoryStream();
            tddos = new BigEndianBinaryWriter(tdbos);
            //MemoryStream tdbis = null;

            MemoryStream fStream = null;

            long currentTick = 0;
            long deltaTick = 0;
            long eventTick = 0;
            int runningStatus = -1;

            // -----------------------------
            // Write each event in the track
            // -----------------------------
            for (int i = 0; i < size; i++) {
                MidiEvent evnt = track.get(i);

                int status;
                int eventtype;
#pragma warning disable 0168
                int metatype;
#pragma warning restore 0168
                int data1, data2;
                int length;
                byte[] data = null;
                ShortMessage shortMessage = null;
                MetaMessage metaMessage = null;
                SysexMessage sysexMessage = null;

                // get the tick
                // $$jb: this gets easier if we change all system-wide time to delta ticks
                eventTick = evnt.getTick();
                deltaTick = evnt.getTick() - currentTick;
                currentTick = evnt.getTick();

                // get the status byte
                status = evnt.getMessage().getStatus();
                eventtype = getType(status);

                switch (eventtype) {
                    case ONE_BYTE:
                        shortMessage = (ShortMessage)evnt.getMessage();
                        data1 = shortMessage.getData1();
                        bytesWritten += writeVarInt(deltaTick);

                        if (status != runningStatus) {
                            runningStatus = status;
                            tddos.Write((byte)status); bytesWritten += 1;
                        }
                        tddos.Write((byte)data1); bytesWritten += 1;
                        break;

                    case TWO_BYTE:
                        shortMessage = (ShortMessage)evnt.getMessage();
                        data1 = shortMessage.getData1();
                        data2 = shortMessage.getData2();

                        bytesWritten += writeVarInt(deltaTick);
                        if (status != runningStatus) {
                            runningStatus = status;
                            tddos.Write((byte)status); bytesWritten += 1;
                        }
                        tddos.Write((byte)data1); bytesWritten += 1;
                        tddos.Write((byte)data2); bytesWritten += 1;
                        break;

                    case SYSEX:
                        sysexMessage = (SysexMessage)evnt.getMessage();
                        length = sysexMessage.getLength();
                        data = sysexMessage.getMessage();
                        bytesWritten += writeVarInt(deltaTick);

                        // $$jb: 04.08.99: always write status for sysex
                        runningStatus = status;
                        tddos.Write(data[0]); bytesWritten += 1;

                        // $$jb: 10.18.99: we don't maintain length in
                        // the message data for SysEx (it is not transmitted
                        // over the line), so write the calculated length
                        // minus the status byte
                        bytesWritten += writeVarInt((data.Length - 1));

                        // $$jb: 10.18.99: now write the rest of the
                        // message
                        tddos.Write(data, 1, (data.Length - 1));
                        bytesWritten += (data.Length - 1);
                        break;

                    case META:
                        metaMessage = (MetaMessage)evnt.getMessage();
                        length = metaMessage.getLength();
                        data = metaMessage.getMessage();
                        bytesWritten += writeVarInt(deltaTick);

                        // $$jb: 10.18.99: getMessage() returns the
                        // entire valid midi message for a file, 
                        // including the status byte and the var-length-int 
                        // length value, so we can just write the data
                        // here.  note that we must _always_ write the
                        // status byte, regardless of runningStatus.
                        runningStatus = status;
                        tddos.Write(data, 0, data.Length);
                        bytesWritten += data.Length;
                        break;

                    case IGNORE:
                        // ignore this event
                        break;

                    case ERROR:
                        // ignore this event
                        break;

                    default:
                        throw new InvalidMidiDataException("internal file writer error");
                }
            }
            // ---------------------------------
            // End write each event in the track
            // ---------------------------------

            // Build Track header now that we know length
            thdos.Write(MTrk_MAGIC);
            thdos.Write(bytesWritten);
            bytesWritten += 8;

            // Now sequence them
            fStream = SunFileWriter.CreateConcatStream(thpos.ToArray(), tdbos);
            thdos.Close();
            tddos.Close();

            return fStream;
        }
    }
}
