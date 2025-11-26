/*
 * Copyright (c) 1999, 2024, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.BufferedInputStream;
//import java.io.DataInputStream;
//import java.io.EOFException;
//import java.io.File;
//import java.io.FileInputStream;
//import java.io.IOException;
//import java.io.InputStream;
//import java.net.URL;

//import javax.sound.midi.InvalidMidiDataException;
//import javax.sound.midi.MetaMessage;
//import javax.sound.midi.MidiEvent;
//import javax.sound.midi.MidiFileFormat;
//import javax.sound.midi.MidiMessage;
//import javax.sound.midi.Sequence;
//import javax.sound.midi.SysexMessage;
//import javax.sound.midi.Track;
//import javax.sound.midi.spi.MidiFileReader;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Midi;
using SystemX.Addon;

namespace SystemX.Media.Sound {

/**
 * MIDI file reader.
 *
 * @author Kara Kytle
 * @author Jan Borgersen
 * @author Florian Bomers
 */
    public sealed class StandardMidiFileReader : MidiFileReader {

        private const int MThd_MAGIC = 0x4d546864;  // 'MThd'

        private const int bisBufferSize = 1024; // buffer size in buffered input streams

        public override MidiFileFormat getMidiFileFormat(Stream stream) {
            return getMidiFileFormatFromStream(stream, MidiFileFormat.UNKNOWN_LENGTH, null);
        }

        // $$fb 2002-04-17: part of fix for 4635286: MidiSystem.getMidiFileFormat()
        // returns format having invalid length
        private MidiFileFormat getMidiFileFormatFromStream(Stream stream,
                                                           int fileLength,
                                                           SMFParser smfParser) {
#pragma warning disable 0219
            int maxReadLength = 16;
#pragma warning restore 0219
            int duration = MidiFileFormat.UNKNOWN_LENGTH;
            BigEndianBinaryReader dis;

            //if (stream is BinaryReader) {
            //    dis = (BinaryReader)stream;
            //} else {
            dis = new BigEndianBinaryReader(stream);
            //}
            long markPosition = 0;
            if (smfParser == null) {
                markPosition = dis.BaseStream.Position; //dis.mark(maxReadLength);
            } else {
                smfParser.stream = dis;
            }

            int type;
            int numtracks;
            float divisionType;
            int resolution;

            try {
                int magic = dis.ReadInt32();
                if (!(magic == MThd_MAGIC)) {
                    // not MIDI
                    throw new InvalidMidiDataException("not a valid MIDI file");
                }

                // read header length
                int bytesRemaining = dis.ReadInt32() - 6;
                type = dis.ReadInt16();
                numtracks = dis.ReadInt16();
                int timing = dis.ReadInt16();

                // decipher the timing code
                if (timing > 0) {
                    // tempo based timing.  value is ticks per beat.
                    divisionType = Sequence.PPQ;
                    resolution = timing;
                } else {
                    // SMPTE based timing.  first decipher the frame code.
                    int frameCode = -1 * (timing >> 8);
                    switch (frameCode) {
                        case 24:
                            divisionType = Sequence.SMPTE_24;
                            break;
                        case 25:
                            divisionType = Sequence.SMPTE_25;
                            break;
                        case 29:
                            divisionType = Sequence.SMPTE_30DROP;
                            break;
                        case 30:
                            divisionType = Sequence.SMPTE_30;
                            break;
                        default:
                            throw new InvalidMidiDataException("Unknown frame code: " + frameCode);
                    }
                    // now determine the timing resolution in ticks per frame.
                    resolution = timing & 0xFF;
                }
                if (smfParser != null) {
                    // remainder of this chunk
                    byte[] b = dis.ReadBytes(bytesRemaining);
                    smfParser.tracks = numtracks;
                }
            } finally {
                // if only reading the file format, reset the stream
                if (smfParser == null) {
                    dis.BaseStream.Position = markPosition; //dis.reset();
                }
            }
            MidiFileFormat format = new MidiFileFormat(type, divisionType, resolution, fileLength, duration);
            return format;
        }


        public override MidiFileFormat getMidiFileFormat(Uri url) {
            using (Stream urlStream = UrlHelper.openStream(url)) // throws IOException
            using (BufferedStream bis = new BufferedStream(urlStream, bisBufferSize)) {
                MidiFileFormat fileFormat = getMidiFileFormat(bis); // throws InvalidMidiDataException
                return fileFormat;
            }
        }


        public override MidiFileFormat getMidiFileFormat(FileInfo file) {
            using (FileStream fis = new FileStream(file.FullName, FileMode.Open, FileAccess.Read)) // throws IOException
            using (BufferedStream bis = new BufferedStream(fis, bisBufferSize)) {

                // $$fb 2002-04-17: part of fix for 4635286: MidiSystem.getMidiFileFormat() returns format having invalid length
                long length = file.Length;
                if (length > Int32.MaxValue) {
                    length = MidiFileFormat.UNKNOWN_LENGTH;
                }
                MidiFileFormat fileFormat = getMidiFileFormatFromStream(bis, (int)length, null);
                return fileFormat;
            }
        }


        public override Sequence getSequence(Stream stream) {
            SMFParser smfParser = new SMFParser();
            MidiFileFormat format = getMidiFileFormatFromStream(stream,
                                        MidiFileFormat.UNKNOWN_LENGTH,
                                        smfParser);

            // must be MIDI Type 0 or Type 1
            if ((format.getType() != 0) && (format.getType() != 1)) {
                throw new InvalidMidiDataException("Invalid or unsupported file type: " + format.getType());
            }

            // construct the sequence object
            Sequence sequence = new Sequence(format.getDivisionType(), format.getResolution());

            // for each track, go to the beginning and read the track events
            for (int i = 0; i < smfParser.tracks; i++) {
                if (smfParser.nextTrack()) {
                    smfParser.readTrack(sequence.createTrack());
                } else {
                    break;
                }
            }
            return sequence;
        }



        public override Sequence getSequence(Uri url) {
            using (Stream instream = UrlHelper.openStream(url))  // throws IOException
            using (Stream bis = new BufferedStream(instream, bisBufferSize)) {
                Sequence seq = getSequence(bis);
                return seq;
            }
        }


        public override Sequence getSequence(FileInfo file) {
            using (Stream instream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read)) // throws IOException
            using (Stream bis = new BufferedStream(instream, bisBufferSize)) {
                Sequence seq = getSequence(bis);
                return seq;
            }
        }
    }

    //=============================================================================================================

    /**
     * State variables during parsing of a MIDI file.
     */
    internal sealed class SMFParser {
        private const int MTrk_MAGIC = 0x4d54726b;  // 'MTrk'

        // set to true to not allow corrupt MIDI files tombe loaded
        private const bool STRICT_PARSER = false;

        private const bool DEBUG = false;

        internal int tracks;                       // number of tracks
        internal BigEndianBinaryReader stream;   // the stream to read from

        private int trackLength = 0;  // remaining length in track
        private byte[] trackData = null;
        private int pos = 0;

        internal SMFParser() {
        }

        private int readUnsigned() {
            return trackData[pos++] & 0xFF;
        }

        private void read(byte[] data) {
            Array.Copy(trackData, pos, data, 0, data.Length);
            pos += data.Length;
        }

        private long readVarInt() {
            long value = 0;  // the variable-lengh int value
            int currentByte = 0;
            do {
                currentByte = trackData[pos++] & 0xFF;
                value = (value << 7) + (currentByte & 0x7F);
            } while ((currentByte & 0x80) != 0);
            return value;
        }

        private int readIntFromStream() {
            try {
                return stream.ReadInt32();
            } catch (EndOfStreamException) {
                throw new EndOfStreamException("invalid MIDI file");
            }
        }

        internal bool nextTrack() {
            int magic;
            trackLength = 0;
            do {
                // $$fb 2003-08-20: fix for 4910986: MIDI file parser breaks up on http connection
                byte[] bytes = stream.ReadBytes(trackLength);
                if (bytes.Length != trackLength) {
                    if (!STRICT_PARSER) {
                        return false;
                    }
                    throw new EndOfStreamException("invalid MIDI file");
                }
                magic = readIntFromStream();
                trackLength = readIntFromStream();
            } while (magic != MTrk_MAGIC);
            if (!STRICT_PARSER) {
                if (trackLength < 0) {
                    return false;
                }
            }
            // now read track in a byte array
            try {
                trackData = new byte[trackLength];
            } catch (OutOfMemoryException oom) {
                throw new IOException("Track length too big", oom);
            }
            try {
                // $$fb 2003-08-20: fix for 4910986: MIDI file parser breaks up on http connection
                trackData = stream.ReadBytes(trackLength);
                if (trackData.Length < trackLength) {
                    throw new EndOfStreamException();
                }
            } catch (EndOfStreamException) {
                if (!STRICT_PARSER) {
                    return false;
                }
                throw new EndOfStreamException("invalid MIDI file");
            }
            pos = 0;
            return true;
        }

        private bool trackFinished() {
            return pos >= trackLength;
        }

        internal void readTrack(Track track) {
            try {
                // reset current tick to 0
                long tick = 0;

                // reset current running status byte to 0 (invalid value).
                // this should cause us to throw an InvalidMidiDataException if we don't
                // get a valid status byte from the beginning of the track.
                int runningStatus = 0;
                bool endOfTrackFound = false;

                while (!trackFinished() && !endOfTrackFound) {
                    MidiMessage message;

                    int data1 = -1;        // initialize to invalid value
                    int data2 = 0;

                    // each event has a tick delay and then the event data.

                    // first read the delay (a variable-length int) and update our tick value
                    tick += readVarInt();

                    // check for new status
                    int byteValue = readUnsigned();

                    int status;
                    if (byteValue >= 0x80) {
                        status = byteValue;

                        // update running status (only for channel messages)
                        if ((status & 0xF0) != 0xF0) {
                            runningStatus = status;
                        }
                    } else {
                        status = runningStatus;
                        data1 = byteValue;
                    }

                    switch (status & 0xF0) {
                        case 0x80:
                        case 0x90:
                        case 0xA0:
                        case 0xB0:
                        case 0xE0:
                            // two data bytes
                            if (data1 == -1) {
                                data1 = readUnsigned();
                            }
                            data2 = readUnsigned();
                            message = new FastShortMessage(status | (data1 << 8) | (data2 << 16));
                            break;
                        case 0xC0:
                        case 0xD0:
                            // one data byte
                            if (data1 == -1) {
                                data1 = readUnsigned();
                            }
                            message = new FastShortMessage(status | (data1 << 8));
                            break;
                        case 0xF0:
                            // sys-ex or meta
                            switch (status) {
                                case 0xF0:
                                case 0xF7:
                                    // sys ex
                                    int sysexLength = (int)readVarInt();
                                    if (sysexLength < 0 || sysexLength > trackLength - pos) {
                                        throw new InvalidMidiDataException("Message length is out of bounds: "
                                                + sysexLength);
                                    }

                                    byte[] sysexData = new byte[sysexLength];
                                    read(sysexData);

                                    SysexMessage sysexMessage = new SysexMessage();
                                    sysexMessage.setMessage(status, sysexData, sysexLength);
                                    message = sysexMessage;
                                    break;

                                case 0xFF:
                                    // meta
                                    int metaType = readUnsigned();
                                    int metaLength = (int)readVarInt();
                                    if (metaLength < 0 || metaLength > trackLength - pos) {
                                        throw new InvalidMidiDataException("Message length is out of bounds: "
                                                + metaLength);
                                    }
                                    byte[] metaData;
                                    try {
                                        metaData = new byte[metaLength];
                                    } catch (OutOfMemoryException oom) {
                                        throw new IOException("Meta length too big", oom);
                                    }
                                    read(metaData);

                                    MetaMessage metaMessage = new MetaMessage();
                                    metaMessage.setMessage(metaType, metaData, metaLength);
                                    message = metaMessage;
                                    if (metaType == 0x2F) {
                                        // end of track means it!
                                        endOfTrackFound = true;
                                    }
                                    break;
                                default:
                                    throw new InvalidMidiDataException("Invalid status byte: " + status);
                            } // switch sys-ex or meta
                            break;
                        default:
                            throw new InvalidMidiDataException("Invalid status byte: " + status);
                    } // switch
                    track.add(new MidiEvent(message, tick));
                } // while
            } catch (IndexOutOfRangeException e) {
                String m = e.Message;
#pragma warning disable 0162
                if (DEBUG)
                    printStackTrace(e);
#pragma warning restore 0162

                // fix for 4834374
                throw new EndOfStreamException("invalid MIDI file");
            }
        }

        private void printStackTrace(Exception ex) {
            Printer.printStackTrace(ex);
        }
    }
}
