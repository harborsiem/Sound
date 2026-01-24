/*
 * Copyright (c) 2007, 2021, Oracle and/or its affiliates. All rights reserved.
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
//import java.io.FileInputStream;
//import java.io.IOException;
//import java.io.InputStream;
//import java.io.OutputStream;
//import java.net.URL;
//import java.util.ArrayDeque;
//import java.util.ArrayList;
//import java.util.Arrays;
//import java.util.HashMap;
//import java.util.List;
//import java.util.Map;

//import javax.sound.midi.Instrument;
//import javax.sound.midi.Patch;
//import javax.sound.midi.Soundbank;
//import javax.sound.midi.SoundbankResource;
//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioFormat.Encoding;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;

//import static java.nio.charset.StandardCharsets.US_ASCII;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Midi;
using SystemX.Sound.Sampled;
using SystemX.Addon;

namespace SystemX.Media.Sound {

/**
 * A DLS Level 1 and Level 2 soundbank reader (from files/url/streams).
 *
 * @author Karl Helgason
 */
    public sealed class DLSSoundbank : ISoundbank {

        private class DLSID {
            long i1;
            int s1;
            int s2;
            int x1;
            int x2;
            int x3;
            int x4;
            int x5;
            int x6;
            int x7;
            int x8;

            private DLSID() {
            }

            internal DLSID(long i1, int s1, int s2, int x1, int x2, int x3, int x4,
                    int x5, int x6, int x7, int x8) {
                this.i1 = i1;
                this.s1 = s1;
                this.s2 = s2;
                this.x1 = x1;
                this.x2 = x2;
                this.x3 = x3;
                this.x4 = x4;
                this.x5 = x5;
                this.x6 = x6;
                this.x7 = x7;
                this.x8 = x8;
            }

            public static DLSID read(RIFFReader riff) {
                DLSID d = new DLSID();
                d.i1 = riff.readUnsignedInt();
                d.s1 = riff.readUnsignedShort();
                d.s2 = riff.readUnsignedShort();
                d.x1 = riff.readUnsignedByte();
                d.x2 = riff.readUnsignedByte();
                d.x3 = riff.readUnsignedByte();
                d.x4 = riff.readUnsignedByte();
                d.x5 = riff.readUnsignedByte();
                d.x6 = riff.readUnsignedByte();
                d.x7 = riff.readUnsignedByte();
                d.x8 = riff.readUnsignedByte();
                return d;
            }

            public override int GetHashCode() {
                return i1.GetHashCode();
            }

            public override bool Equals(Object obj) {
                if (!(obj is DLSID)) {
                    return false;
                }
                DLSID t = (DLSID)obj;
                return i1 == t.i1 && s1 == t.s1 && s2 == t.s2
                    && x1 == t.x1 && x2 == t.x2 && x3 == t.x3 && x4 == t.x4
                    && x5 == t.x5 && x6 == t.x6 && x7 == t.x7 && x8 == t.x8;
            }
        }

        /** X = X & Y */
        private const int DLS_CDL_AND = 0x0001;
        /** X = X | Y */
        private const int DLS_CDL_OR = 0x0002;
        /** X = X ^ Y */
        private const int DLS_CDL_XOR = 0x0003;
        /** X = X + Y */
        private const int DLS_CDL_ADD = 0x0004;
        /** X = X - Y */
        private const int DLS_CDL_SUBTRACT = 0x0005;
        /** X = X * Y */
        private const int DLS_CDL_MULTIPLY = 0x0006;
        /** X = X / Y */
        private const int DLS_CDL_DIVIDE = 0x0007;
        /** X = X && Y */
        private const int DLS_CDL_LOGICAL_AND = 0x0008;
        /** X = X || Y */
        private const int DLS_CDL_LOGICAL_OR = 0x0009;
        /** X = (X < Y) */
        private const int DLS_CDL_LT = 0x000A;
        /** X = (X <= Y) */
        private const int DLS_CDL_LE = 0x000B;
        /** X = (X > Y) */
        private const int DLS_CDL_GT = 0x000C;
        /** X = (X >= Y) */
        private const int DLS_CDL_GE = 0x000D;
        /** X = (X == Y) */
        private const int DLS_CDL_EQ = 0x000E;
        /** X = !X */
        private const int DLS_CDL_NOT = 0x000F;
        /** 32-bit constant */
        private const int DLS_CDL_CONST = 0x0010;
        /** 32-bit value returned from query */
        private const int DLS_CDL_QUERY = 0x0011;
        /** 32-bit value returned from query */
        private const int DLS_CDL_QUERYSUPPORTED = 0x0012;

        private static readonly DLSID DLSID_GMInHardware = new DLSID(0x178f2f24,
                0xc364, 0x11d1, 0xa7, 0x60, 0x00, 0x00, 0xf8, 0x75, 0xac, 0x12);
        private static readonly DLSID DLSID_GSInHardware = new DLSID(0x178f2f25,
                0xc364, 0x11d1, 0xa7, 0x60, 0x00, 0x00, 0xf8, 0x75, 0xac, 0x12);
        private static readonly DLSID DLSID_XGInHardware = new DLSID(0x178f2f26,
                0xc364, 0x11d1, 0xa7, 0x60, 0x00, 0x00, 0xf8, 0x75, 0xac, 0x12);
        private static readonly DLSID DLSID_SupportsDLS1 = new DLSID(0x178f2f27,
                0xc364, 0x11d1, 0xa7, 0x60, 0x00, 0x00, 0xf8, 0x75, 0xac, 0x12);
        private static readonly DLSID DLSID_SupportsDLS2 = new DLSID(0xf14599e5,
                0x4689, 0x11d2, 0xaf, 0xa6, 0x0, 0xaa, 0x0, 0x24, 0xd8, 0xb6);
        private static readonly DLSID DLSID_SampleMemorySize = new DLSID(0x178f2f28,
                0xc364, 0x11d1, 0xa7, 0x60, 0x00, 0x00, 0xf8, 0x75, 0xac, 0x12);
        private static readonly DLSID DLSID_ManufacturersID = new DLSID(0xb03e1181,
                0x8095, 0x11d2, 0xa1, 0xef, 0x0, 0x60, 0x8, 0x33, 0xdb, 0xd8);
        private static readonly DLSID DLSID_ProductID = new DLSID(0xb03e1182,
                0x8095, 0x11d2, 0xa1, 0xef, 0x0, 0x60, 0x8, 0x33, 0xdb, 0xd8);
        private static readonly DLSID DLSID_SamplePlaybackRate = new DLSID(0x2a91f713,
                0xa4bf, 0x11d2, 0xbb, 0xdf, 0x0, 0x60, 0x8, 0x33, 0xdb, 0xd8);

        private long major = -1;
        private long minor = -1;

        private readonly DLSInfo info = new DLSInfo();

        private readonly List<DLSInstrument> instruments = new List<DLSInstrument>();
        private readonly List<DLSSample> samples = new List<DLSSample>();

        private bool largeFormat = false;
        private FileInfo sampleFile;

        public DLSSoundbank() {
        }

        public DLSSoundbank(Uri url) {
            using (Stream istream = UrlHelper.openStream(url)) {
                readSoundbank(istream);
            }
        }

        public DLSSoundbank(FileInfo file) {
            largeFormat = true;
            sampleFile = file;
            using (Stream istream = file.OpenRead()) {
                readSoundbank(istream);
            }
        }

        public DLSSoundbank(Stream inputstream) {
            readSoundbank(inputstream);
        }

        private void readSoundbank(Stream inputstream) {
            RIFFReader riff = new RIFFReader(inputstream);
            if (!riff.getFormat().Equals("RIFF")) {
                throw new RIFFInvalidFormatException(
                        "Input stream is not a valid RIFF stream!");
            }
            if (!riff.getRiffType().Equals("DLS ")) {
                throw new RIFFInvalidFormatException(
                        "Input stream is not a valid DLS soundbank!");
            }
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                if (chunk.getFormat().Equals("LIST")) {
                    if (chunk.getRiffType().Equals("INFO"))
                        readInfoChunk(chunk);
                    if (chunk.getRiffType().Equals("lins"))
                        readLinsChunk(chunk);
                    if (chunk.getRiffType().Equals("wvpl"))
                        readWvplChunk(chunk);
                } else {
                    if (chunk.getFormat().Equals("cdl ")) {
                        if (!readCdlChunk(chunk)) {
                            throw new RIFFInvalidFormatException(
                                    "DLS file isn't supported!");
                        }
                    }
                    if (chunk.getFormat().Equals("colh")) {
                        // skipped because we will load the entire bank into memory
                        // long instrumentcount = chunk.readUnsignedInt();
                        // System.out.println("instrumentcount = "+ instrumentcount);
                    }
                    if (chunk.getFormat().Equals("ptbl")) {
                        // Pool Table Chunk
                        // skipped because we will load the entire bank into memory
                    }
                    if (chunk.getFormat().Equals("vers")) {
                        major = chunk.readUnsignedInt();
                        minor = chunk.readUnsignedInt();
                    }
                }
            }

            foreach (KeyValuePair<DLSRegion, Int64> entry in temp_rgnassign) {
                entry.Key.sample = samples[(int)entry.Value];
            }

            temp_rgnassign = null;
        }

        private bool cdlIsQuerySupported(DLSID uuid) {
            return uuid.Equals(DLSID_GMInHardware)
                || uuid.Equals(DLSID_GSInHardware)
                || uuid.Equals(DLSID_XGInHardware)
                || uuid.Equals(DLSID_SupportsDLS1)
                || uuid.Equals(DLSID_SupportsDLS2)
                || uuid.Equals(DLSID_SampleMemorySize)
                || uuid.Equals(DLSID_ManufacturersID)
                || uuid.Equals(DLSID_ProductID)
                || uuid.Equals(DLSID_SamplePlaybackRate);
        }

        private long cdlQuery(DLSID uuid) {
            if (uuid.Equals(DLSID_GMInHardware))
                return 1;
            if (uuid.Equals(DLSID_GSInHardware))
                return 0;
            if (uuid.Equals(DLSID_XGInHardware))
                return 0;
            if (uuid.Equals(DLSID_SupportsDLS1))
                return 1;
            if (uuid.Equals(DLSID_SupportsDLS2))
                return 1;
            if (uuid.Equals(DLSID_SampleMemorySize))
                return Environment.WorkingSet; //Runtime.getRuntime().totalMemory()
            if (uuid.Equals(DLSID_ManufacturersID))
                return 0;
            if (uuid.Equals(DLSID_ProductID))
                return 0;
            if (uuid.Equals(DLSID_SamplePlaybackRate))
                return 44100;
            return 0;
        }


        // Reading cdl-ck Chunk
        // "cdl " chunk can only appear inside : DLS,lart,lar2,rgn,rgn2
        private bool readCdlChunk(RIFFReader riff) {

            DLSID uuid;
            long x;
            long y;
            Stack<Int64> stack = new Stack<Int64>();

            while (riff.available() != 0) {
                int opcode = riff.readUnsignedShort();
                switch (opcode) {
                    case DLS_CDL_AND:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push((((x != 0) && (y != 0)) ? 1 : 0));
                        break;
                    case DLS_CDL_OR:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push((((x != 0) || (y != 0)) ? 1 : 0));
                        break;
                    case DLS_CDL_XOR:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push((((x != 0) ^ (y != 0)) ? 1 : 0));
                        break;
                    case DLS_CDL_ADD:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push((x + y));
                        break;
                    case DLS_CDL_SUBTRACT:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push((x - y));
                        break;
                    case DLS_CDL_MULTIPLY:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push((x * y));
                        break;
                    case DLS_CDL_DIVIDE:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push((x / y));
                        break;
                    case DLS_CDL_LOGICAL_AND:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push((((x != 0) && (y != 0)) ? 1 : 0));
                        break;
                    case DLS_CDL_LOGICAL_OR:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push((((x != 0) || (y != 0)) ? 1 : 0));
                        break;
                    case DLS_CDL_LT:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push(((x < y) ? 1 : 0));
                        break;
                    case DLS_CDL_LE:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push(((x <= y) ? 1 : 0));
                        break;
                    case DLS_CDL_GT:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push(((x > y) ? 1 : 0));
                        break;
                    case DLS_CDL_GE:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push(((x >= y) ? 1 : 0));
                        break;
                    case DLS_CDL_EQ:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push(((x == y) ? 1 : 0));
                        break;
                    case DLS_CDL_NOT:
                        x = stack.Pop();
                        y = stack.Pop();
                        stack.Push(((x == 0) ? 1 : 0));
                        break;
                    case DLS_CDL_CONST:
                        stack.Push((riff.readUnsignedInt()));
                        break;
                    case DLS_CDL_QUERY:
                        uuid = DLSID.read(riff);
                        stack.Push(cdlQuery(uuid));
                        break;
                    case DLS_CDL_QUERYSUPPORTED:
                        uuid = DLSID.read(riff);
                        stack.Push((cdlIsQuerySupported(uuid) ? 1 : 0));
                        break;
                    default:
                        break;
                }
            }
            if (stack.Count == 0)
                return false;

            return stack.Pop() == 1;
        }

        private void readInfoChunk(RIFFReader riff) {
            info.name = null;
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                String format = chunk.getFormat();
                if (format.Equals("INAM"))
                    info.name = chunk.readString(chunk.available());
                else if (format.Equals("ICRD"))
                    info.creationDate = chunk.readString(chunk.available());
                else if (format.Equals("IENG"))
                    info.engineers = chunk.readString(chunk.available());
                else if (format.Equals("IPRD"))
                    info.product = chunk.readString(chunk.available());
                else if (format.Equals("ICOP"))
                    info.copyright = chunk.readString(chunk.available());
                else if (format.Equals("ICMT"))
                    info.comments = chunk.readString(chunk.available());
                else if (format.Equals("ISFT"))
                    info.tools = chunk.readString(chunk.available());
                else if (format.Equals("IARL"))
                    info.archival_location = chunk.readString(chunk.available());
                else if (format.Equals("IART"))
                    info.artist = chunk.readString(chunk.available());
                else if (format.Equals("ICMS"))
                    info.commissioned = chunk.readString(chunk.available());
                else if (format.Equals("IGNR"))
                    info.genre = chunk.readString(chunk.available());
                else if (format.Equals("IKEY"))
                    info.keywords = chunk.readString(chunk.available());
                else if (format.Equals("IMED"))
                    info.medium = chunk.readString(chunk.available());
                else if (format.Equals("ISBJ"))
                    info.subject = chunk.readString(chunk.available());
                else if (format.Equals("ISRC"))
                    info.source = chunk.readString(chunk.available());
                else if (format.Equals("ISRF"))
                    info.source_form = chunk.readString(chunk.available());
                else if (format.Equals("ITCH"))
                    info.technician = chunk.readString(chunk.available());
            }
        }

        private void readLinsChunk(RIFFReader riff) {
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                if (chunk.getFormat().Equals("LIST")) {
                    if (chunk.getRiffType().Equals("ins "))
                        readInsChunk(chunk);
                }
            }
        }

        private void readInsChunk(RIFFReader riff) {
            DLSInstrument instrument = new DLSInstrument(this);

            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                String format = chunk.getFormat();
                if (format.Equals("LIST")) {
                    if (chunk.getRiffType().Equals("INFO")) {
                        readInsInfoChunk(instrument, chunk);
                    }
                    if (chunk.getRiffType().Equals("lrgn")) {
                        while (chunk.hasNextChunk()) {
                            RIFFReader subchunk = chunk.nextChunk();
                            if (subchunk.getFormat().Equals("LIST")) {
                                if (subchunk.getRiffType().Equals("rgn ")) {
                                    DLSRegion split = new DLSRegion();
                                    if (readRgnChunk(split, subchunk))
                                        instrument.getRegions().Add(split);
                                }
                                if (subchunk.getRiffType().Equals("rgn2")) {
                                    // support for DLS level 2 regions
                                    DLSRegion split = new DLSRegion();
                                    if (readRgnChunk(split, subchunk))
                                        instrument.getRegions().Add(split);
                                }
                            }
                        }
                    }
                    if (chunk.getRiffType().Equals("lart")) {
                        List<DLSModulator> modlist = new List<DLSModulator>();
                        while (chunk.hasNextChunk()) {
                            RIFFReader subchunk = chunk.nextChunk();
                            if (chunk.getFormat().Equals("cdl ")) {
                                if (!readCdlChunk(chunk)) {
                                    modlist.Clear();
                                    break;
                                }
                            }
                            if (subchunk.getFormat().Equals("art1"))
                                readArt1Chunk(modlist, subchunk);
                        }
                        ((List<DLSModulator>)instrument.getModulators()).AddRange(modlist);
                    }
                    if (chunk.getRiffType().Equals("lar2")) {
                        // support for DLS level 2 ART
                        List<DLSModulator> modlist = new List<DLSModulator>();
                        while (chunk.hasNextChunk()) {
                            RIFFReader subchunk = chunk.nextChunk();
                            if (chunk.getFormat().Equals("cdl ")) {
                                if (!readCdlChunk(chunk)) {
                                    modlist.Clear();
                                    break;
                                }
                            }
                            if (subchunk.getFormat().Equals("art2"))
                                readArt2Chunk(modlist, subchunk);
                        }
                        ((List<DLSModulator>)instrument.getModulators()).AddRange(modlist);
                    }
                } else {
                    if (format.Equals("dlid")) {
                        instrument.guid = new byte[16];
                        chunk.readFully(instrument.guid);
                    }
                    if (format.Equals("insh")) {
                        chunk.readUnsignedInt(); // Read Region Count - ignored

                        int bank = chunk.ReadByte();             // LSB
                        bank += (chunk.ReadByte() & 127) << 7;   // MSB
                        chunk.ReadByte(); // Read Reserved byte
                        int drumins = chunk.ReadByte();          // Drum Instrument

                        int id = chunk.ReadByte() & 127; // Read only first 7 bits
                        chunk.ReadByte(); // Read Reserved byte
                        chunk.ReadByte(); // Read Reserved byte
                        chunk.ReadByte(); // Read Reserved byte

                        instrument.bank = bank;
                        instrument.preset = id;
                        instrument.druminstrument = (drumins & 128) > 0;
                        //System.out.println("bank="+bank+" drumkit="+drumkit
                        //        +" id="+id);
                    }

                }
            }
            instruments.Add(instrument);
        }

        private void readArt1Chunk(List<DLSModulator> modulators, RIFFReader riff) {
            long size = riff.readUnsignedInt();
            long count = riff.readUnsignedInt();

            if (size - 8 != 0)
                riff.skip(size - 8);

            for (int i = 0; i < count; i++) {
                DLSModulator modulator = new DLSModulator();
                modulator.version = 1;
                modulator.source = riff.readUnsignedShort();
                modulator.control = riff.readUnsignedShort();
                modulator.destination = riff.readUnsignedShort();
                modulator.transform = riff.readUnsignedShort();
                modulator.scale = riff.readInt();
                modulators.Add(modulator);
            }
        }

        private void readArt2Chunk(List<DLSModulator> modulators, RIFFReader riff) {
            long size = riff.readUnsignedInt();
            long count = riff.readUnsignedInt();

            if (size - 8 != 0)
                riff.skip(size - 8);

            for (int i = 0; i < count; i++) {
                DLSModulator modulator = new DLSModulator();
                modulator.version = 2;
                modulator.source = riff.readUnsignedShort();
                modulator.control = riff.readUnsignedShort();
                modulator.destination = riff.readUnsignedShort();
                modulator.transform = riff.readUnsignedShort();
                modulator.scale = riff.readInt();
                modulators.Add(modulator);
            }
        }

        private Dictionary<DLSRegion, Int64> temp_rgnassign = new Dictionary<DLSRegion, Int64>();

        private bool readRgnChunk(DLSRegion split, RIFFReader riff) {
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                String format = chunk.getFormat();
                if (format.Equals("LIST")) {
                    if (chunk.getRiffType().Equals("lart")) {
                        List<DLSModulator> modlist = new List<DLSModulator>();
                        while (chunk.hasNextChunk()) {
                            RIFFReader subchunk = chunk.nextChunk();
                            if (chunk.getFormat().Equals("cdl ")) {
                                if (!readCdlChunk(chunk)) {
                                    modlist.Clear();
                                    break;
                                }
                            }
                            if (subchunk.getFormat().Equals("art1"))
                                readArt1Chunk(modlist, subchunk);
                        }
                        ((List<DLSModulator>)split.getModulators()).AddRange(modlist);
                    }
                    if (chunk.getRiffType().Equals("lar2")) {
                        // support for DLS level 2 ART
                        List<DLSModulator> modlist = new List<DLSModulator>();
                        while (chunk.hasNextChunk()) {
                            RIFFReader subchunk = chunk.nextChunk();
                            if (chunk.getFormat().Equals("cdl ")) {
                                if (!readCdlChunk(chunk)) {
                                    modlist.Clear();
                                    break;
                                }
                            }
                            if (subchunk.getFormat().Equals("art2"))
                                readArt2Chunk(modlist, subchunk);
                        }
                        ((List<DLSModulator>)split.getModulators()).AddRange(modlist);
                    }
                } else {

                    if (format.Equals("cdl ")) {
                        if (!readCdlChunk(chunk))
                            return false;
                    }
                    if (format.Equals("rgnh")) {
                        split.keyfrom = chunk.readUnsignedShort();
                        split.keyto = chunk.readUnsignedShort();
                        split.velfrom = chunk.readUnsignedShort();
                        split.velto = chunk.readUnsignedShort();
                        split.options = chunk.readUnsignedShort();
                        split.exclusiveClass = chunk.readUnsignedShort();
                    }
                    if (format.Equals("wlnk")) {
                        split.fusoptions = chunk.readUnsignedShort();
                        split.phasegroup = chunk.readUnsignedShort();
                        split.channel = chunk.readUnsignedInt();
                        long sampleid = chunk.readUnsignedInt();
                        temp_rgnassign[split] = sampleid;
                    }
                    if (format.Equals("wsmp")) {
                        split.sampleoptions = new DLSSampleOptions();
                        readWsmpChunk(split.sampleoptions, chunk);
                    }
                }
            }
            return true;
        }

        private void readWsmpChunk(DLSSampleOptions sampleOptions, RIFFReader riff) {
            long size = riff.readUnsignedInt();
            sampleOptions.unitynote = riff.readUnsignedShort();
            sampleOptions.finetune = riff.readShort();
            sampleOptions.attenuation = riff.readInt();
            sampleOptions.options = riff.readUnsignedInt();
            long loops = riff.readInt();

            if (size > 20)
                riff.skip(size - 20);

            for (int i = 0; i < loops; i++) {
                DLSSampleLoop loop = new DLSSampleLoop();
                long size2 = riff.readUnsignedInt();
                loop.type = riff.readUnsignedInt();
                loop.start = riff.readUnsignedInt();
                loop.length = riff.readUnsignedInt();
                sampleOptions.loops.Add(loop);
                if (size2 > 16)
                    riff.skip(size2 - 16);
            }
        }

        private void readInsInfoChunk(DLSInstrument dlsinstrument, RIFFReader riff) {
            dlsinstrument.info.name = null;
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                String format = chunk.getFormat();
                if (format.Equals("INAM")) {
                    dlsinstrument.info.name = chunk.readString(chunk.available());
                } else if (format.Equals("ICRD")) {
                    dlsinstrument.info.creationDate =
                            chunk.readString(chunk.available());
                } else if (format.Equals("IENG")) {
                    dlsinstrument.info.engineers =
                            chunk.readString(chunk.available());
                } else if (format.Equals("IPRD")) {
                    dlsinstrument.info.product = chunk.readString(chunk.available());
                } else if (format.Equals("ICOP")) {
                    dlsinstrument.info.copyright =
                            chunk.readString(chunk.available());
                } else if (format.Equals("ICMT")) {
                    dlsinstrument.info.comments =
                            chunk.readString(chunk.available());
                } else if (format.Equals("ISFT")) {
                    dlsinstrument.info.tools = chunk.readString(chunk.available());
                } else if (format.Equals("IARL")) {
                    dlsinstrument.info.archival_location =
                            chunk.readString(chunk.available());
                } else if (format.Equals("IART")) {
                    dlsinstrument.info.artist = chunk.readString(chunk.available());
                } else if (format.Equals("ICMS")) {
                    dlsinstrument.info.commissioned =
                            chunk.readString(chunk.available());
                } else if (format.Equals("IGNR")) {
                    dlsinstrument.info.genre = chunk.readString(chunk.available());
                } else if (format.Equals("IKEY")) {
                    dlsinstrument.info.keywords =
                            chunk.readString(chunk.available());
                } else if (format.Equals("IMED")) {
                    dlsinstrument.info.medium = chunk.readString(chunk.available());
                } else if (format.Equals("ISBJ")) {
                    dlsinstrument.info.subject = chunk.readString(chunk.available());
                } else if (format.Equals("ISRC")) {
                    dlsinstrument.info.source = chunk.readString(chunk.available());
                } else if (format.Equals("ISRF")) {
                    dlsinstrument.info.source_form =
                            chunk.readString(chunk.available());
                } else if (format.Equals("ITCH")) {
                    dlsinstrument.info.technician =
                            chunk.readString(chunk.available());
                }
            }
        }

        private void readWvplChunk(RIFFReader riff) {
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                if (chunk.getFormat().Equals("LIST")) {
                    if (chunk.getRiffType().Equals("wave"))
                        readWaveChunk(chunk);
                }
            }
        }

        private void readWaveChunk(RIFFReader riff) {
            DLSSample sample = new DLSSample(this);

            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                String format = chunk.getFormat();
                if (format.Equals("LIST")) {
                    if (chunk.getRiffType().Equals("INFO")) {
                        readWaveInfoChunk(sample, chunk);
                    }
                } else {
                    if (format.Equals("dlid")) {
                        sample.guid = new byte[16];
                        chunk.readFully(sample.guid);
                    }

                    if (format.Equals("fmt ")) {
                        int sampleformat = chunk.readUnsignedShort();
                        if (sampleformat != 1 && sampleformat != 3) {
                            throw new RIFFInvalidDataException(
                                    "Only PCM samples are supported!");
                        }
                        int channels = chunk.readUnsignedShort();
                        long samplerate = chunk.readUnsignedInt();
                        // bytes per sec
                        /* long framerate = */
                        chunk.readUnsignedInt();
                        // block align, framesize
                        int framesize = chunk.readUnsignedShort();
                        int bits = chunk.readUnsignedShort();
                        AudioFormat audioformat = null;
                        if (sampleformat == 1) {
                            if (bits == 8) {
                                audioformat = new AudioFormat(
                                        AudioFormat.Encoding.PCM_UNSIGNED, samplerate, bits,
                                        channels, framesize, samplerate, false);
                            } else {
                                audioformat = new AudioFormat(
                                        AudioFormat.Encoding.PCM_SIGNED, samplerate, bits,
                                        channels, framesize, samplerate, false);
                            }
                        }
                        if (sampleformat == 3) {
                            audioformat = new AudioFormat(
                                    AudioFormat.Encoding.PCM_FLOAT, samplerate, bits,
                                    channels, framesize, samplerate, false);
                        }

                        sample.format = audioformat;
                    }

                    if (format.Equals("data")) {
                        if (largeFormat) {
                            sample.setData(new ModelByteBuffer(sampleFile,
                                    chunk.getFilePointer(), chunk.available()));
                        } else {
                            byte[] buffer = new byte[chunk.available()];
                            //  chunk.read(buffer);
                            sample.setData(buffer);

                            int read = 0;
                            int avail = chunk.available();
                            while (read != avail) {
                                if (avail - read > 65536) {
                                    chunk.readFully(buffer, read, 65536);
                                    read += 65536;
                                } else {
                                    chunk.readFully(buffer, read, avail - read);
                                    read = avail;
                                }
                            }
                        }
                    }

                    if (format.Equals("wsmp")) {
                        sample.sampleoptions = new DLSSampleOptions();
                        readWsmpChunk(sample.sampleoptions, chunk);
                    }
                }
            }

            samples.Add(sample);

        }

        private void readWaveInfoChunk(DLSSample dlssample, RIFFReader riff) {
            dlssample.info.name = null;
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                String format = chunk.getFormat();
                if (format.Equals("INAM")) {
                    dlssample.info.name = chunk.readString(chunk.available());
                } else if (format.Equals("ICRD")) {
                    dlssample.info.creationDate =
                            chunk.readString(chunk.available());
                } else if (format.Equals("IENG")) {
                    dlssample.info.engineers = chunk.readString(chunk.available());
                } else if (format.Equals("IPRD")) {
                    dlssample.info.product = chunk.readString(chunk.available());
                } else if (format.Equals("ICOP")) {
                    dlssample.info.copyright = chunk.readString(chunk.available());
                } else if (format.Equals("ICMT")) {
                    dlssample.info.comments = chunk.readString(chunk.available());
                } else if (format.Equals("ISFT")) {
                    dlssample.info.tools = chunk.readString(chunk.available());
                } else if (format.Equals("IARL")) {
                    dlssample.info.archival_location =
                            chunk.readString(chunk.available());
                } else if (format.Equals("IART")) {
                    dlssample.info.artist = chunk.readString(chunk.available());
                } else if (format.Equals("ICMS")) {
                    dlssample.info.commissioned =
                            chunk.readString(chunk.available());
                } else if (format.Equals("IGNR")) {
                    dlssample.info.genre = chunk.readString(chunk.available());
                } else if (format.Equals("IKEY")) {
                    dlssample.info.keywords = chunk.readString(chunk.available());
                } else if (format.Equals("IMED")) {
                    dlssample.info.medium = chunk.readString(chunk.available());
                } else if (format.Equals("ISBJ")) {
                    dlssample.info.subject = chunk.readString(chunk.available());
                } else if (format.Equals("ISRC")) {
                    dlssample.info.source = chunk.readString(chunk.available());
                } else if (format.Equals("ISRF")) {
                    dlssample.info.source_form = chunk.readString(chunk.available());
                } else if (format.Equals("ITCH")) {
                    dlssample.info.technician = chunk.readString(chunk.available());
                }
            }
        }

        public void save(String name) {
            using (RIFFWriter writer = new RIFFWriter(name, "DLS ")) {
                writeSoundbank(writer);
            }
        }

        public void save(FileInfo file) {
            using (RIFFWriter writer = new RIFFWriter(file, "DLS ")) {
                writeSoundbank(writer);
            }
        }

        public void save(Stream output) {
            using (RIFFWriter writer = new RIFFWriter(output, "DLS ")) {
                writeSoundbank(writer);
            }
        }

        private void writeSoundbank(RIFFWriter writer) {
            RIFFWriter colh_chunk = writer.writeChunk("colh");
            colh_chunk.writeUnsignedInt(instruments.Count);

            if (major != -1 && minor != -1) {
                RIFFWriter vers_chunk = writer.writeChunk("vers");
                vers_chunk.writeUnsignedInt(major);
                vers_chunk.writeUnsignedInt(minor);
            }

            writeInstruments(writer.writeList("lins"));

            RIFFWriter ptbl = writer.writeChunk("ptbl");
            ptbl.writeUnsignedInt(8);
            ptbl.writeUnsignedInt(samples.Count);
            long ptbl_offset = writer.getFilePointer();
            for (int i = 0; i < samples.Count; i++)
                ptbl.writeUnsignedInt(0);

            RIFFWriter wvpl = writer.writeList("wvpl");
            long off = wvpl.getFilePointer();
            List<Int64> offsettable = new List<Int64>();
            foreach (DLSSample sample in samples) {
                offsettable.Add((wvpl.getFilePointer() - off));
                writeSample(wvpl.writeList("wave"), sample);
            }

            // small cheat, we are going to rewrite data back in wvpl
            long bak = writer.getFilePointer();
            writer.Seek(ptbl_offset);
            writer.setWriteOverride(true);
            foreach (Int64 offset in offsettable)
                writer.writeUnsignedInt(offset);
            writer.setWriteOverride(false);
            writer.Seek(bak);

            writeInfo(writer.writeList("INFO"), info);
        }

        private void writeSample(RIFFWriter writer, DLSSample sample) {

            AudioFormat audioformat = sample.getFormat();

            AudioFormat.Encoding encoding = audioformat.getEncoding();
            float sampleRate = audioformat.getSampleRate();
            int sampleSizeInBits = audioformat.getSampleSizeInBits();
            int channels = audioformat.getChannels();
            int frameSize = audioformat.getFrameSize();
            float frameRate = audioformat.getFrameRate();
            bool bigEndian = audioformat.isBigEndian();

            bool convert_needed = false;

            if (audioformat.getSampleSizeInBits() == 8) {
                if (!encoding.Equals(AudioFormat.Encoding.PCM_UNSIGNED)) {
                    encoding = AudioFormat.Encoding.PCM_UNSIGNED;
                    convert_needed = true;
                }
            } else {
                if (!encoding.Equals(AudioFormat.Encoding.PCM_SIGNED)) {
                    encoding = AudioFormat.Encoding.PCM_SIGNED;
                    convert_needed = true;
                }
                if (bigEndian) {
                    bigEndian = false;
                    convert_needed = true;
                }
            }

            if (convert_needed) {
                audioformat = new AudioFormat(encoding, sampleRate,
                        sampleSizeInBits, channels, frameSize, frameRate, bigEndian);
            }

            // fmt
            RIFFWriter fmt_chunk = writer.writeChunk("fmt ");
            int sampleformat = 0;
            if (audioformat.getEncoding().Equals(AudioFormat.Encoding.PCM_UNSIGNED))
                sampleformat = 1;
            else if (audioformat.getEncoding().Equals(AudioFormat.Encoding.PCM_SIGNED))
                sampleformat = 1;
            else if (audioformat.getEncoding().Equals(AudioFormat.Encoding.PCM_FLOAT))
                sampleformat = 3;

            fmt_chunk.writeUnsignedShort(sampleformat);
            fmt_chunk.writeUnsignedShort(audioformat.getChannels());
            fmt_chunk.writeUnsignedInt((long)audioformat.getSampleRate());
            long srate = ((long)audioformat.getFrameRate()) * audioformat.getFrameSize();
            fmt_chunk.writeUnsignedInt(srate);
            fmt_chunk.writeUnsignedShort(audioformat.getFrameSize());
            fmt_chunk.writeUnsignedShort(audioformat.getSampleSizeInBits());
            fmt_chunk.Write(0);
            fmt_chunk.Write(0);

            writeSampleOptions(writer.writeChunk("wsmp"), sample.sampleoptions);

            if (convert_needed) {
                RIFFWriter data_chunk = writer.writeChunk("data");
                AudioInputStream stream = AudioSystem.getAudioInputStream(
                        audioformat, (AudioInputStream)sample.getData());
                stream.transferTo(data_chunk);
            } else {
                RIFFWriter data_chunk = writer.writeChunk("data");
                ModelByteBuffer databuff = sample.getDataBuffer();
                databuff.writeTo(data_chunk);
                /*
                data_chunk.write(databuff.array(),
                databuff.arrayOffset(),
                databuff.capacity());
                 */
            }

            writeInfo(writer.writeList("INFO"), sample.info);
        }

        private void writeInstruments(RIFFWriter writer) {
            foreach (DLSInstrument instrument in instruments) {
                writeInstrument(writer.writeList("ins "), instrument);
            }
        }

        private void writeInstrument(RIFFWriter writer, DLSInstrument instrument) {

            int art1_count = 0;
            int art2_count = 0;
            foreach (DLSModulator modulator in instrument.getModulators()) {
                if (modulator.version == 1)
                    art1_count++;
                if (modulator.version == 2)
                    art2_count++;
            }
            foreach (DLSRegion region in instrument.regions) {
                foreach (DLSModulator modulator in region.getModulators()) {
                    if (modulator.version == 1)
                        art1_count++;
                    if (modulator.version == 2)
                        art2_count++;
                }
            }

            int version = 1;
            if (art2_count > 0)
                version = 2;

            RIFFWriter insh_chunk = writer.writeChunk("insh");
            insh_chunk.writeUnsignedInt(instrument.getRegions().Count);
            insh_chunk.writeUnsignedInt(instrument.bank +
                    (instrument.druminstrument ? 2147483648L : 0));
            insh_chunk.writeUnsignedInt(instrument.preset);

            RIFFWriter lrgn = writer.writeList("lrgn");
            foreach (DLSRegion region in instrument.regions)
                writeRegion(lrgn, region, version);

            writeArticulators(writer, instrument.getModulators());

            writeInfo(writer.writeList("INFO"), instrument.info);

        }

        private void writeArticulators(RIFFWriter writer,
                IList<DLSModulator> modulators) {
            int art1_count = 0;
            int art2_count = 0;
            foreach (DLSModulator modulator in modulators) {
                if (modulator.version == 1)
                    art1_count++;
                if (modulator.version == 2)
                    art2_count++;
            }
            if (art1_count > 0) {
                RIFFWriter lar1 = writer.writeList("lart");
                RIFFWriter art1 = lar1.writeChunk("art1");
                art1.writeUnsignedInt(8);
                art1.writeUnsignedInt(art1_count);
                foreach (DLSModulator modulator in modulators) {
                    if (modulator.version == 1) {
                        art1.writeUnsignedShort(modulator.source);
                        art1.writeUnsignedShort(modulator.control);
                        art1.writeUnsignedShort(modulator.destination);
                        art1.writeUnsignedShort(modulator.transform);
                        art1.writeInt(modulator.scale);
                    }
                }
            }
            if (art2_count > 0) {
                RIFFWriter lar2 = writer.writeList("lar2");
                RIFFWriter art2 = lar2.writeChunk("art2");
                art2.writeUnsignedInt(8);
                art2.writeUnsignedInt(art2_count);
                foreach (DLSModulator modulator in modulators) {
                    if (modulator.version == 2) {
                        art2.writeUnsignedShort(modulator.source);
                        art2.writeUnsignedShort(modulator.control);
                        art2.writeUnsignedShort(modulator.destination);
                        art2.writeUnsignedShort(modulator.transform);
                        art2.writeInt(modulator.scale);
                    }
                }
            }
        }

        private void writeRegion(RIFFWriter writer, DLSRegion region, int version) {
            RIFFWriter rgns = null;
            if (version == 1)
                rgns = writer.writeList("rgn ");
            if (version == 2)
                rgns = writer.writeList("rgn2");
            if (rgns == null)
                return;

            RIFFWriter rgnh = rgns.writeChunk("rgnh");
            rgnh.writeUnsignedShort(region.keyfrom);
            rgnh.writeUnsignedShort(region.keyto);
            rgnh.writeUnsignedShort(region.velfrom);
            rgnh.writeUnsignedShort(region.velto);
            rgnh.writeUnsignedShort(region.options);
            rgnh.writeUnsignedShort(region.exclusiveClass);

            if (region.sampleoptions != null)
                writeSampleOptions(rgns.writeChunk("wsmp"), region.sampleoptions);

            if (region.sample != null) {
                if (samples.IndexOf(region.sample) != -1) {
                    RIFFWriter wlnk = rgns.writeChunk("wlnk");
                    wlnk.writeUnsignedShort(region.fusoptions);
                    wlnk.writeUnsignedShort(region.phasegroup);
                    wlnk.writeUnsignedInt(region.channel);
                    wlnk.writeUnsignedInt(samples.IndexOf(region.sample));
                }
            }
            writeArticulators(rgns, region.getModulators());
            rgns.Close();
        }

        private void writeSampleOptions(RIFFWriter wsmp,
                DLSSampleOptions sampleoptions) {
            wsmp.writeUnsignedInt(20);
            wsmp.writeUnsignedShort(sampleoptions.unitynote);
            wsmp.writeShort(sampleoptions.finetune);
            wsmp.writeInt(sampleoptions.attenuation);
            wsmp.writeUnsignedInt(sampleoptions.options);
            wsmp.writeInt(sampleoptions.loops.Count);

            foreach (DLSSampleLoop loop in sampleoptions.loops) {
                wsmp.writeUnsignedInt(16);
                wsmp.writeUnsignedInt(loop.type);
                wsmp.writeUnsignedInt(loop.start);
                wsmp.writeUnsignedInt(loop.length);
            }
        }

        private void writeInfoStringChunk(RIFFWriter writer,
                String name, String value) {
            if (value == null)
                return;
            RIFFWriter chunk = writer.writeChunk(name);
            chunk.writeString(value);
            int len = Encoding.ASCII.GetBytes(value).Length;
            chunk.Write(0);
            len++;
            if (len % 2 != 0)
                chunk.Write(0);
        }

        private void writeInfo(RIFFWriter writer, DLSInfo info) {
            writeInfoStringChunk(writer, "INAM", info.name);
            writeInfoStringChunk(writer, "ICRD", info.creationDate);
            writeInfoStringChunk(writer, "IENG", info.engineers);
            writeInfoStringChunk(writer, "IPRD", info.product);
            writeInfoStringChunk(writer, "ICOP", info.copyright);
            writeInfoStringChunk(writer, "ICMT", info.comments);
            writeInfoStringChunk(writer, "ISFT", info.tools);
            writeInfoStringChunk(writer, "IARL", info.archival_location);
            writeInfoStringChunk(writer, "IART", info.artist);
            writeInfoStringChunk(writer, "ICMS", info.commissioned);
            writeInfoStringChunk(writer, "IGNR", info.genre);
            writeInfoStringChunk(writer, "IKEY", info.keywords);
            writeInfoStringChunk(writer, "IMED", info.medium);
            writeInfoStringChunk(writer, "ISBJ", info.subject);
            writeInfoStringChunk(writer, "ISRC", info.source);
            writeInfoStringChunk(writer, "ISRF", info.source_form);
            writeInfoStringChunk(writer, "ITCH", info.technician);
        }

        public DLSInfo getInfo() {
            return info;
        }

        public String getName() {
            return info.name;
        }

        public String getVersion() {
            return major + "." + minor;
        }

        public String getVendor() {
            return info.engineers;
        }

        public String getDescription() {
            return info.comments;
        }

        public void setName(String s) {
            info.name = s;
        }

        public void setVendor(String s) {
            info.engineers = s;
        }

        public void setDescription(String s) {
            info.comments = s;
        }

        public SoundbankResource[] getResources() {
            SoundbankResource[] resources = new SoundbankResource[samples.Count];
            int j = 0;
            for (int i = 0; i < samples.Count; i++)
                resources[j++] = samples[i];
            return resources;
        }

        public Instrument[] getInstruments() {
            DLSInstrument[] inslist_array =
                    instruments.ToArray();
            Array.Sort(inslist_array, new ModelInstrumentComparator());
            return inslist_array;
        }

        public DLSSample[] getSamples() {
            return samples.ToArray();
        }

        public Instrument getInstrument(Patch patch) {
            int program = patch.getProgram();
            int bank = patch.getBank();
            bool percussion = false;
            if (patch is ModelPatch)
                percussion = ((ModelPatch)patch).isPercussion();
            foreach (Instrument instrument in instruments) {
                Patch patch2 = instrument.getPatch();
                int program2 = patch2.getProgram();
                int bank2 = patch2.getBank();
                if (program == program2 && bank == bank2) {
                    bool percussion2 = false;
                    if (patch2 is ModelPatch)
                        percussion2 = ((ModelPatch)patch2).isPercussion();
                    if (percussion == percussion2)
                        return instrument;
                }
            }
            return null;
        }

        public void addResource(SoundbankResource resource) {
            if (resource is DLSInstrument)
                instruments.Add((DLSInstrument)resource);
            if (resource is DLSSample)
                samples.Add((DLSSample)resource);
        }

        public void removeResource(SoundbankResource resource) {
            if (resource is DLSInstrument)
                instruments.Remove((DLSInstrument)resource);
            if (resource is DLSSample)
                samples.Remove((DLSSample)resource);
        }

        public void addInstrument(DLSInstrument resource) {
            instruments.Add(resource);
        }

        public void removeInstrument(DLSInstrument resource) {
            instruments.Remove(resource);
        }

        public long getMajor() {
            return major;
        }

        public void setMajor(long major) {
            this.major = major;
        }

        public long getMinor() {
            return minor;
        }

        public void setMinor(long minor) {
            this.minor = minor;
        }

    }
}
