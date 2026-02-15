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
//import java.util.ArrayList;
//import java.util.Arrays;
//import java.util.Iterator;
//import java.util.List;
//import java.util.Map;

//import javax.sound.midi.Instrument;
//import javax.sound.midi.Patch;
//import javax.sound.midi.Soundbank;
//import javax.sound.midi.SoundbankResource;

//import static java.nio.charset.StandardCharsets.US_ASCII;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using SystemX.Sound.Midi;
using SystemX.Addon;

namespace SystemX.Media.Sound {

/**
 * A SoundFont 2.04 soundbank reader.
 *
 * Based on SoundFont 2.04 specification from:
 * <p>  http://developer.creative.com <br>
 *      http://www.soundfont.com/ ;
 *
 * @author Karl Helgason
 */
    public sealed class SF2Soundbank : ISoundbank {

        // version of the Sound Font RIFF file
        internal int major = 2;
        internal int minor = 1;
        // target Sound Engine
        internal String targetEngine = "EMU8000";
        // Sound Font Bank Name
        internal String name = "untitled";
        // Sound ROM Name
        internal String romName = null;
        // Sound ROM Version
        internal int romVersionMajor = -1;
        internal int romVersionMinor = -1;
        // Date of Creation of the Bank
        internal String creationDate = null;
        // Sound Designers and Engineers for the Bank
        internal String engineers = null;
        // Product for which the Bank was intended
        internal String product = null;
        // Copyright message
        internal String copyright = null;
        // Comments
        internal String comments = null;
        // The SoundFont tools used to create and alter the bank
        internal String tools = null;
        // The Sample Data loaded from the SoundFont
        private ModelByteBuffer sampleData = null;
        private ModelByteBuffer sampleData24 = null;
        private FileInfo sampleFile = null;
        private bool largeFormat = false;
        private readonly List<SF2Instrument> instruments = new List<SF2Instrument>();
        private readonly List<SF2Layer> layers = new List<SF2Layer>();
        private readonly List<SF2Sample> samples = new List<SF2Sample>();

        public SF2Soundbank() {
        }

        public SF2Soundbank(Uri url) {
            using (Stream istream = UrlHelper.openStream(url)) {
                readSoundbank(istream);
            }
        }

        public SF2Soundbank(FileInfo file) {
            largeFormat = true;
            sampleFile = file;
            using (Stream istream = file.OpenRead()) {
                readSoundbank(istream);
            }
        }

        public SF2Soundbank(Stream inputstream) {
            readSoundbank(inputstream);
        }

        private void readSoundbank(Stream inputstream) {
            RIFFReader riff = new RIFFReader(inputstream);
            if (!riff.getFormat().Equals("RIFF")) {
                throw new RIFFInvalidFormatException(
                        "Input stream is not a valid RIFF stream!");
            }
            if (!riff.getRiffType().Equals("sfbk")) {
                throw new RIFFInvalidFormatException(
                        "Input stream is not a valid SoundFont!");
            }
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                if (chunk.getFormat().Equals("LIST")) {
                    if (chunk.getRiffType().Equals("INFO"))
                        readInfoChunk(chunk);
                    if (chunk.getRiffType().Equals("sdta"))
                        readSdtaChunk(chunk);
                    if (chunk.getRiffType().Equals("pdta"))
                        readPdtaChunk(chunk);
                }
            }
        }

        private void readInfoChunk(RIFFReader riff) {
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                String format = chunk.getFormat();
                if (format.Equals("ifil")) {
                    major = chunk.readUnsignedShort();
                    minor = chunk.readUnsignedShort();
                } else if (format.Equals("isng")) {
                    this.targetEngine = chunk.readString(chunk.available());
                } else if (format.Equals("INAM")) {
                    this.name = chunk.readString(chunk.available());
                } else if (format.Equals("irom")) {
                    this.romName = chunk.readString(chunk.available());
                } else if (format.Equals("iver")) {
                    romVersionMajor = chunk.readUnsignedShort();
                    romVersionMinor = chunk.readUnsignedShort();
                } else if (format.Equals("ICRD")) {
                    this.creationDate = chunk.readString(chunk.available());
                } else if (format.Equals("IENG")) {
                    this.engineers = chunk.readString(chunk.available());
                } else if (format.Equals("IPRD")) {
                    this.product = chunk.readString(chunk.available());
                } else if (format.Equals("ICOP")) {
                    this.copyright = chunk.readString(chunk.available());
                } else if (format.Equals("ICMT")) {
                    this.comments = chunk.readString(chunk.available());
                } else if (format.Equals("ISFT")) {
                    this.tools = chunk.readString(chunk.available());
                }

            }
        }

        private void readSdtaChunk(RIFFReader riff) {
            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                if (chunk.getFormat().Equals("smpl")) {
                    if (!largeFormat) {
                        byte[] sampleData = new byte[chunk.available()];

                        int read = 0;
                        int avail = chunk.available();
                        while (read != avail) {
                            if (avail - read > 65536) {
                                chunk.readFully(sampleData, read, 65536);
                                read += 65536;
                            } else {
                                chunk.readFully(sampleData, read, avail - read);
                                read = avail;
                            }

                        }
                        this.sampleData = new ModelByteBuffer(sampleData);
                        //chunk.read(sampleData);
                    } else {
                        this.sampleData = new ModelByteBuffer(sampleFile,
                                chunk.getFilePointer(), chunk.available());
                    }
                }
                if (chunk.getFormat().Equals("sm24")) {
                    if (!largeFormat) {
                        byte[] sampleData24 = new byte[chunk.available()];
                        //chunk.read(sampleData24);

                        int read = 0;
                        int avail = chunk.available();
                        while (read != avail) {
                            if (avail - read > 65536) {
                                chunk.readFully(sampleData24, read, 65536);
                                read += 65536;
                            } else {
                                chunk.readFully(sampleData24, read, avail - read);
                                read = avail;
                            }
                        }
                        this.sampleData24 = new ModelByteBuffer(sampleData24);
                    } else {
                        this.sampleData24 = new ModelByteBuffer(sampleFile,
                                chunk.getFilePointer(), chunk.available());
                    }
                }
            }
        }

        private void readPdtaChunk(RIFFReader riff) {

            List<SF2Instrument> presets = new List<SF2Instrument>();
            List<Int32> presets_bagNdx = new List<Int32>();
            List<SF2InstrumentRegion> presets_splits_gen
                    = new List<SF2InstrumentRegion>();
            List<SF2InstrumentRegion> presets_splits_mod = new List<SF2InstrumentRegion>();

            List<SF2Layer> instruments = new List<SF2Layer>();
            List<Int32> instruments_bagNdx = new List<Int32>();
            List<SF2LayerRegion> instruments_splits_gen
                    = new List<SF2LayerRegion>();
            List<SF2LayerRegion> instruments_splits_mod = new List<SF2LayerRegion>();

            while (riff.hasNextChunk()) {
                RIFFReader chunk = riff.nextChunk();
                String format = chunk.getFormat();
                if (format.Equals("phdr")) {
                    // Preset Header / Instrument
                    if (chunk.available() % 38 != 0)
                        throw new RIFFInvalidDataException();
                    int count = chunk.available() / 38;
                    for (int i = 0; i < count; i++) {
                        SF2Instrument preset = new SF2Instrument(this);
                        preset.name = chunk.readString(20);
                        preset.preset = chunk.readUnsignedShort();
                        preset.bank = chunk.readUnsignedShort();
                        presets_bagNdx.Add(chunk.readUnsignedShort());
                        preset.library = chunk.readUnsignedInt();
                        preset.genre = chunk.readUnsignedInt();
                        preset.morphology = chunk.readUnsignedInt();
                        presets.Add(preset);
                        if (i != count - 1)
                            this.instruments.Add(preset);
                    }
                } else if (format.Equals("pbag")) {
                    // Preset Zones / Instruments splits
                    if (chunk.available() % 4 != 0)
                        throw new RIFFInvalidDataException();
                    int count = chunk.available() / 4;

                    // Skip first record
                    {
                        int gencount = chunk.readUnsignedShort();
                        int modcount = chunk.readUnsignedShort();
                        while (presets_splits_gen.Count < gencount)
                            presets_splits_gen.Add(null);
                        while (presets_splits_mod.Count < modcount)
                            presets_splits_mod.Add(null);
                        count--;
                    }

                    if (presets_bagNdx.Count == 0) {
                        throw new RIFFInvalidDataException();
                    }
                    int offset = presets_bagNdx[0];
                    // Offset should be 0 (but just case)
                    for (int i = 0; i < offset; i++) {
                        if (count == 0)
                            throw new RIFFInvalidDataException();
                        int gencount = chunk.readUnsignedShort();
                        int modcount = chunk.readUnsignedShort();
                        while (presets_splits_gen.Count < gencount)
                            presets_splits_gen.Add(null);
                        while (presets_splits_mod.Count < modcount)
                            presets_splits_mod.Add(null);
                        count--;
                    }

                    for (int i = 0; i < presets_bagNdx.Count - 1; i++) {
                        int zone_count = presets_bagNdx[i + 1]
                                         - presets_bagNdx[i];
                        SF2Instrument preset = presets[i];
                        for (int ii = 0; ii < zone_count; ii++) {
                            if (count == 0)
                                throw new RIFFInvalidDataException();
                            int gencount = chunk.readUnsignedShort();
                            int modcount = chunk.readUnsignedShort();
                            SF2InstrumentRegion split = new SF2InstrumentRegion();
                            preset.regions.Add(split);
                            while (presets_splits_gen.Count < gencount)
                                presets_splits_gen.Add(split);
                            while (presets_splits_mod.Count < modcount)
                                presets_splits_mod.Add(split);
                            count--;
                        }
                    }
                } else if (format.Equals("pmod")) {
                    // Preset Modulators / Split Modulators
                    for (int i = 0; i < presets_splits_mod.Count; i++) {
                        SF2Modulator modulator = new SF2Modulator();
                        modulator.sourceOperator = chunk.readUnsignedShort();
                        modulator.destinationOperator = chunk.readUnsignedShort();
                        modulator.amount = chunk.readShort();
                        modulator.amountSourceOperator = chunk.readUnsignedShort();
                        modulator.transportOperator = chunk.readUnsignedShort();
                        SF2InstrumentRegion split = presets_splits_mod[i];
                        if (split != null)
                            split.modulators.Add(modulator);
                    }
                } else if (format.Equals("pgen")) {
                    // Preset Generators / Split Generators
                    for (int i = 0; i < presets_splits_gen.Count; i++) {
                        int operator0 = chunk.readUnsignedShort();
                        short amount = chunk.readShort();
                        SF2InstrumentRegion split = presets_splits_gen[i];
                        if (split != null)
                            split.generators[operator0] = amount;
                    }
                } else if (format.Equals("inst")) {
                    // Instrument Header / Layers
                    if (chunk.available() % 22 != 0)
                        throw new RIFFInvalidDataException();
                    int count = chunk.available() / 22;
                    for (int i = 0; i < count; i++) {
                        SF2Layer layer = new SF2Layer(this);
                        layer.name = chunk.readString(20);
                        instruments_bagNdx.Add(chunk.readUnsignedShort());
                        instruments.Add(layer);
                        if (i != count - 1)
                            this.layers.Add(layer);
                    }
                } else if (format.Equals("ibag")) {
                    // Instrument Zones / Layer splits
                    if (chunk.available() % 4 != 0)
                        throw new RIFFInvalidDataException();
                    int count = chunk.available() / 4;

                    // Skip first record
                    {
                        int gencount = chunk.readUnsignedShort();
                        int modcount = chunk.readUnsignedShort();
                        while (instruments_splits_gen.Count < gencount)
                            instruments_splits_gen.Add(null);
                        while (instruments_splits_mod.Count < modcount)
                            instruments_splits_mod.Add(null);
                        count--;
                    }

                    if (instruments_bagNdx.Count == 0) {
                        throw new RIFFInvalidDataException();
                    }
                    int offset = instruments_bagNdx[0];
                    // Offset should be 0 (but just case)
                    for (int i = 0; i < offset; i++) {
                        if (count == 0)
                            throw new RIFFInvalidDataException();
                        int gencount = chunk.readUnsignedShort();
                        int modcount = chunk.readUnsignedShort();
                        while (instruments_splits_gen.Count < gencount)
                            instruments_splits_gen.Add(null);
                        while (instruments_splits_mod.Count < modcount)
                            instruments_splits_mod.Add(null);
                        count--;
                    }

                    for (int i = 0; i < instruments_bagNdx.Count - 1; i++) {
                        int zone_count = instruments_bagNdx[i + 1] - instruments_bagNdx[i];
                        SF2Layer layer = layers[i];
                        for (int ii = 0; ii < zone_count; ii++) {
                            if (count == 0)
                                throw new RIFFInvalidDataException();
                            int gencount = chunk.readUnsignedShort();
                            int modcount = chunk.readUnsignedShort();
                            SF2LayerRegion split = new SF2LayerRegion();
                            layer.regions.Add(split);
                            while (instruments_splits_gen.Count < gencount)
                                instruments_splits_gen.Add(split);
                            while (instruments_splits_mod.Count < modcount)
                                instruments_splits_mod.Add(split);
                            count--;
                        }
                    }

                } else if (format.Equals("imod")) {
                    // Instrument Modulators / Split Modulators
                    for (int i = 0; i < instruments_splits_mod.Count; i++) {
                        SF2Modulator modulator = new SF2Modulator();
                        modulator.sourceOperator = chunk.readUnsignedShort();
                        modulator.destinationOperator = chunk.readUnsignedShort();
                        modulator.amount = chunk.readShort();
                        modulator.amountSourceOperator = chunk.readUnsignedShort();
                        modulator.transportOperator = chunk.readUnsignedShort();
                        if (i < 0 || i >= instruments_splits_gen.Count) {
                            throw new RIFFInvalidDataException();
                        }
                        SF2LayerRegion split = instruments_splits_gen[i];
                        if (split != null)
                            split.modulators.Add(modulator);
                    }
                } else if (format.Equals("igen")) {
                    // Instrument Generators / Split Generators
                    for (int i = 0; i < instruments_splits_gen.Count; i++) {
                        int operator0 = chunk.readUnsignedShort();
                        short amount = chunk.readShort();
                        SF2LayerRegion split = instruments_splits_gen[i];
                        if (split != null)
                            split.generators[operator0] = amount;
                    }
                } else if (format.Equals("shdr")) {
                    // Sample Headers
                    if (chunk.available() % 46 != 0)
                        throw new RIFFInvalidDataException();
                    int count = chunk.available() / 46;
                    for (int i = 0; i < count; i++) {
                        SF2Sample sample = new SF2Sample(this);
                        sample.name = chunk.readString(20);
                        long start = chunk.readUnsignedInt();
                        long end = chunk.readUnsignedInt();
                        if (sampleData != null)
                            sample.data = sampleData.subbuffer(start * 2, end * 2, true);
                        if (sampleData24 != null)
                            sample.data24 = sampleData24.subbuffer(start, end, true);
                        /*
                        sample.data = new ModelByteBuffer(sampleData, (int)(start*2),
                                (int)((end - start)*2));
                        if (sampleData24 != null)
                            sample.data24 = new ModelByteBuffer(sampleData24,
                                    (int)start, (int)(end - start));
                         */
                        sample.startLoop = chunk.readUnsignedInt() - start;
                        sample.endLoop = chunk.readUnsignedInt() - start;
                        if (sample.startLoop < 0)
                            sample.startLoop = -1;
                        if (sample.endLoop < 0)
                            sample.endLoop = -1;
                        sample.sampleRate = chunk.readUnsignedInt();
                        sample.originalPitch = chunk.readUnsignedByte();
                        sample.pitchCorrection = (sbyte)chunk.readSByte();
                        sample.sampleLink = chunk.readUnsignedShort();
                        sample.sampleType = chunk.readUnsignedShort();
                        if (i != count - 1)
                            this.samples.Add(sample);
                    }
                }
            }

            foreach (SF2Layer layer in this.layers) {
                IEnumerator<SF2LayerRegion> siter = layer.regions.GetEnumerator();
                SF2Region globalsplit = null;
                while (siter.MoveNext()) {
                    SF2LayerRegion split = siter.Current;
                    if (split.generators.ContainsKey(SF2LayerRegion.GENERATOR_SAMPLEID)) {
                        int sampleid = split.generators[
                                SF2LayerRegion.GENERATOR_SAMPLEID];
                        split.generators.Remove(SF2LayerRegion.GENERATOR_SAMPLEID);
                        if (sampleid < 0 || sampleid >= samples.Count) {
                            throw new RIFFInvalidDataException();
                        }
                        split.sample = samples[sampleid];
                    } else {
                        globalsplit = split;
                    }
                }
                if (globalsplit != null) {
                    layer.getRegions().Remove((SF2LayerRegion)globalsplit);
                    SF2GlobalRegion gsplit = new SF2GlobalRegion();
                    gsplit.generators = globalsplit.generators;
                    gsplit.modulators = globalsplit.modulators;
                    layer.setGlobalZone(gsplit);
                }
            }


            foreach (SF2Instrument instrument in this.instruments) {
                IEnumerator<SF2InstrumentRegion> siter = instrument.regions.GetEnumerator();
                SF2Region globalsplit = null;
                while (siter.MoveNext()) {
                    SF2InstrumentRegion split = siter.Current;
                    if (split.generators.ContainsKey(SF2LayerRegion.GENERATOR_INSTRUMENT)) {
                        int instrumentid = split.generators[
                                SF2InstrumentRegion.GENERATOR_INSTRUMENT];
                        split.generators.Remove(SF2LayerRegion.GENERATOR_INSTRUMENT);
                        if (instrumentid < 0 || instrumentid >= layers.Count) {
                            throw new RIFFInvalidDataException();
                        }
                        split.layer = layers[instrumentid];
                    } else {
                        globalsplit = split;
                    }
                }

                if (globalsplit != null) {
                    instrument.getRegions().Remove((SF2InstrumentRegion)globalsplit);
                    SF2GlobalRegion gsplit = new SF2GlobalRegion();
                    gsplit.generators = globalsplit.generators;
                    gsplit.modulators = globalsplit.modulators;
                    instrument.setGlobalZone(gsplit);
                }
            }
        }

        public void save(String name) {
            using (RIFFWriter writer = new RIFFWriter(name, "sfbk")) {
                writeSoundbank(writer);
            }
        }

        public void save(FileInfo file) {
            using (RIFFWriter writer = new RIFFWriter(file, "sfbk")) {
                writeSoundbank(writer);
            }
        }

        public void save(Stream output) {
            using (RIFFWriter writer = new RIFFWriter(output, "sfbk")) {
                writeSoundbank(writer);
            }
        }

        private void writeSoundbank(RIFFWriter writer) {
            writeInfo(writer.writeList("INFO"));
            writeSdtaChunk(writer.writeList("sdta"));
            writePdtaChunk(writer.writeList("pdta"));
        }

        private void writeInfoStringChunk(RIFFWriter writer, String name,
                String value) {
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

        private void writeInfo(RIFFWriter writer) {
            if (this.targetEngine == null)
                this.targetEngine = "EMU8000";
            if (this.name == null)
                this.name = "";

            RIFFWriter ifil_chunk = writer.writeChunk("ifil");
            ifil_chunk.writeUnsignedShort(this.major);
            ifil_chunk.writeUnsignedShort(this.minor);
            writeInfoStringChunk(writer, "isng", this.targetEngine);
            writeInfoStringChunk(writer, "INAM", this.name);
            writeInfoStringChunk(writer, "irom", this.romName);
            if (romVersionMajor != -1) {
                RIFFWriter iver_chunk = writer.writeChunk("iver");
                iver_chunk.writeUnsignedShort(this.romVersionMajor);
                iver_chunk.writeUnsignedShort(this.romVersionMinor);
            }
            writeInfoStringChunk(writer, "ICRD", this.creationDate);
            writeInfoStringChunk(writer, "IENG", this.engineers);
            writeInfoStringChunk(writer, "IPRD", this.product);
            writeInfoStringChunk(writer, "ICOP", this.copyright);
            writeInfoStringChunk(writer, "ICMT", this.comments);
            writeInfoStringChunk(writer, "ISFT", this.tools);

            writer.Close();
        }

        private void writeSdtaChunk(RIFFWriter writer) {

            byte[] pad = new byte[32];

            RIFFWriter smpl_chunk = writer.writeChunk("smpl");
            foreach (SF2Sample sample in samples) {
                ModelByteBuffer data = sample.getDataBuffer();
                data.writeTo(smpl_chunk);
                /*
                smpl_chunk.write(data.array(),
                data.arrayOffset(),
                data.capacity());
                 */
                smpl_chunk.Write(pad);
                smpl_chunk.Write(pad);
            }
            if (major < 2)
                return;
            if (major == 2 && minor < 4)
                return;


            foreach (SF2Sample sample in samples) {
                ModelByteBuffer data24 = sample.getData24Buffer();
                if (data24 == null)
                    return;
            }

            RIFFWriter sm24_chunk = writer.writeChunk("sm24");
            foreach (SF2Sample sample in samples) {
                ModelByteBuffer data = sample.getData24Buffer();
                data.writeTo(sm24_chunk);
                /*
                sm24_chunk.write(data.array(),
                data.arrayOffset(),
                data.capacity());*/
                smpl_chunk.Write(pad);
            }
        }

        private void writeModulators(RIFFWriter writer, IList<SF2Modulator> modulators) {
            foreach (SF2Modulator modulator in modulators) {
                writer.writeUnsignedShort(modulator.sourceOperator);
                writer.writeUnsignedShort(modulator.destinationOperator);
                writer.writeShort(modulator.amount);
                writer.writeUnsignedShort(modulator.amountSourceOperator);
                writer.writeUnsignedShort(modulator.transportOperator);
            }
        }

        private void writeGenerators(RIFFWriter writer, IDictionary<Int32, Int16> generators) {
            bool bKeyrange = generators.ContainsKey(SF2Region.GENERATOR_KEYRANGE); //a@
            bool bVelrange = generators.ContainsKey(SF2Region.GENERATOR_VELRANGE);
            short keyrange = 0;
            short velrange = 0;
            if (bKeyrange) {
                keyrange = generators[SF2Region.GENERATOR_KEYRANGE];
            }
            if (bVelrange) {
                velrange = generators[SF2Region.GENERATOR_VELRANGE];
            }
            if (bKeyrange) { //a@ null
                writer.writeUnsignedShort(SF2Region.GENERATOR_KEYRANGE);
                writer.writeShort(keyrange);
            }
            if (bVelrange) { //a@ null
                writer.writeUnsignedShort(SF2Region.GENERATOR_VELRANGE);
                writer.writeShort(velrange);
            }
            foreach (KeyValuePair<Int32, Int16> generator in generators) {
                if (generator.Key == SF2Region.GENERATOR_KEYRANGE)
                    continue;
                if (generator.Key == SF2Region.GENERATOR_VELRANGE)
                    continue;
                writer.writeUnsignedShort(generator.Key);
                writer.writeShort(generator.Value);
            }
        }

        private void writePdtaChunk(RIFFWriter writer) {

            RIFFWriter phdr_chunk = writer.writeChunk("phdr");
            int phdr_zone_count = 0;
            foreach (SF2Instrument preset in this.instruments) {
                phdr_chunk.writeString(preset.name, 20);
                phdr_chunk.writeUnsignedShort(preset.preset);
                phdr_chunk.writeUnsignedShort(preset.bank);
                phdr_chunk.writeUnsignedShort(phdr_zone_count);
                if (preset.getGlobalRegion() != null)
                    phdr_zone_count += 1;
                phdr_zone_count += preset.getRegions().Count;
                phdr_chunk.writeUnsignedInt(preset.library);
                phdr_chunk.writeUnsignedInt(preset.genre);
                phdr_chunk.writeUnsignedInt(preset.morphology);
            }
            phdr_chunk.writeString("EOP", 20);
            phdr_chunk.writeUnsignedShort(0);
            phdr_chunk.writeUnsignedShort(0);
            phdr_chunk.writeUnsignedShort(phdr_zone_count);
            phdr_chunk.writeUnsignedInt(0);
            phdr_chunk.writeUnsignedInt(0);
            phdr_chunk.writeUnsignedInt(0);


            RIFFWriter pbag_chunk = writer.writeChunk("pbag");
            int pbag_gencount = 0;
            int pbag_modcount = 0;
            foreach (SF2Instrument preset in this.instruments) {
                if (preset.getGlobalRegion() != null) {
                    pbag_chunk.writeUnsignedShort(pbag_gencount);
                    pbag_chunk.writeUnsignedShort(pbag_modcount);
                    pbag_gencount += preset.getGlobalRegion().getGenerators().Count;
                    pbag_modcount += preset.getGlobalRegion().getModulators().Count;
                }
                foreach (SF2InstrumentRegion region in preset.getRegions()) {
                    pbag_chunk.writeUnsignedShort(pbag_gencount);
                    pbag_chunk.writeUnsignedShort(pbag_modcount);
                    if (layers.IndexOf(region.layer) != -1) {
                        // One generator is used to reference to instrument record
                        pbag_gencount += 1;
                    }
                    pbag_gencount += region.getGenerators().Count;
                    pbag_modcount += region.getModulators().Count;

                }
            }
            pbag_chunk.writeUnsignedShort(pbag_gencount);
            pbag_chunk.writeUnsignedShort(pbag_modcount);

            RIFFWriter pmod_chunk = writer.writeChunk("pmod");
            foreach (SF2Instrument preset in this.instruments) {
                if (preset.getGlobalRegion() != null) {
                    writeModulators(pmod_chunk,
                            preset.getGlobalRegion().getModulators());
                }
                foreach (SF2InstrumentRegion region in preset.getRegions())
                    writeModulators(pmod_chunk, region.getModulators());
            }
            pmod_chunk.Write(new byte[10]);

            RIFFWriter pgen_chunk = writer.writeChunk("pgen");
            foreach (SF2Instrument preset in this.instruments) {
                if (preset.getGlobalRegion() != null) {
                    writeGenerators(pgen_chunk,
                            preset.getGlobalRegion().getGenerators());
                }
                foreach (SF2InstrumentRegion region in preset.getRegions()) {
                    writeGenerators(pgen_chunk, region.getGenerators());
                    int ix = layers.IndexOf(region.layer);
                    if (ix != -1) {
                        pgen_chunk.writeUnsignedShort(SF2Region.GENERATOR_INSTRUMENT);
                        pgen_chunk.writeShort((short)ix);
                    }
                }
            }
            pgen_chunk.Write(new byte[4]);

            RIFFWriter inst_chunk = writer.writeChunk("inst");
            int inst_zone_count = 0;
            foreach (SF2Layer instrument in this.layers) {
                inst_chunk.writeString(instrument.name, 20);
                inst_chunk.writeUnsignedShort(inst_zone_count);
                if (instrument.getGlobalRegion() != null)
                    inst_zone_count += 1;
                inst_zone_count += instrument.getRegions().Count;
            }
            inst_chunk.writeString("EOI", 20);
            inst_chunk.writeUnsignedShort(inst_zone_count);


            RIFFWriter ibag_chunk = writer.writeChunk("ibag");
            int ibag_gencount = 0;
            int ibag_modcount = 0;
            foreach (SF2Layer instrument in this.layers) {
                if (instrument.getGlobalRegion() != null) {
                    ibag_chunk.writeUnsignedShort(ibag_gencount);
                    ibag_chunk.writeUnsignedShort(ibag_modcount);
                    ibag_gencount
                            += instrument.getGlobalRegion().getGenerators().Count;
                    ibag_modcount
                            += instrument.getGlobalRegion().getModulators().Count;
                }
                foreach (SF2LayerRegion region in instrument.getRegions()) {
                    ibag_chunk.writeUnsignedShort(ibag_gencount);
                    ibag_chunk.writeUnsignedShort(ibag_modcount);
                    if (samples.IndexOf(region.sample) != -1) {
                        // One generator is used to reference to instrument record
                        ibag_gencount += 1;
                    }
                    ibag_gencount += region.getGenerators().Count;
                    ibag_modcount += region.getModulators().Count;

                }
            }
            ibag_chunk.writeUnsignedShort(ibag_gencount);
            ibag_chunk.writeUnsignedShort(ibag_modcount);


            RIFFWriter imod_chunk = writer.writeChunk("imod");
            foreach (SF2Layer instrument in this.layers) {
                if (instrument.getGlobalRegion() != null) {
                    writeModulators(imod_chunk,
                            instrument.getGlobalRegion().getModulators());
                }
                foreach (SF2LayerRegion region in instrument.getRegions())
                    writeModulators(imod_chunk, region.getModulators());
            }
            imod_chunk.Write(new byte[10]);

            RIFFWriter igen_chunk = writer.writeChunk("igen");
            foreach (SF2Layer instrument in this.layers) {
                if (instrument.getGlobalRegion() != null) {
                    writeGenerators(igen_chunk,
                            instrument.getGlobalRegion().getGenerators());
                }
                foreach (SF2LayerRegion region in instrument.getRegions()) {
                    writeGenerators(igen_chunk, region.getGenerators());
                    int ix = samples.IndexOf(region.sample);
                    if (ix != -1) {
                        igen_chunk.writeUnsignedShort(SF2Region.GENERATOR_SAMPLEID);
                        igen_chunk.writeShort((short)ix);
                    }
                }
            }
            igen_chunk.Write(new byte[4]);


            RIFFWriter shdr_chunk = writer.writeChunk("shdr");
            long sample_pos = 0;
            foreach (SF2Sample sample in samples) {
                shdr_chunk.writeString(sample.name, 20);
                long start = sample_pos;
                sample_pos += sample.data.capacity() / 2;
                long end = sample_pos;
                long startLoop = sample.startLoop + start;
                long endLoop = sample.endLoop + start;
                if (startLoop < start)
                    startLoop = start;
                if (endLoop > end)
                    endLoop = end;
                shdr_chunk.writeUnsignedInt(start);
                shdr_chunk.writeUnsignedInt(end);
                shdr_chunk.writeUnsignedInt(startLoop);
                shdr_chunk.writeUnsignedInt(endLoop);
                shdr_chunk.writeUnsignedInt(sample.sampleRate);
                shdr_chunk.writeUnsignedByte(sample.originalPitch);
                shdr_chunk.WriteSByte(sample.pitchCorrection);
                shdr_chunk.writeUnsignedShort(sample.sampleLink);
                shdr_chunk.writeUnsignedShort(sample.sampleType);
                sample_pos += 32;
            }
            shdr_chunk.writeString("EOS", 20);
            shdr_chunk.Write(new byte[26]);

        }

        public String getName() {
            return name;
        }

        public String getVersion() {
            return major + "." + minor;
        }

        public String getVendor() {
            return engineers;
        }

        public String getDescription() {
            return comments;
        }

        public void setName(String s) {
            name = s;
        }

        public void setVendor(String s) {
            engineers = s;
        }

        public void setDescription(String s) {
            comments = s;
        }

        public SoundbankResource[] getResources() {
            SoundbankResource[] resources
                    = new SoundbankResource[layers.Count + samples.Count];
            int j = 0;
            for (int i = 0; i < layers.Count; i++)
                resources[j++] = layers[i];
            for (int i = 0; i < samples.Count; i++)
                resources[j++] = samples[i];
            return resources;
        }

        public Instrument[] getInstruments() { //SF2Instrument
            SF2Instrument[] inslist_array
                    = instruments.ToArray();
            Array.Sort(inslist_array, new ModelInstrumentComparator());
            return inslist_array;
        }

        public SF2Layer[] getLayers() {
            return layers.ToArray();
        }

        public SF2Sample[] getSamples() {
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

        public String getCreationDate() {
            return creationDate;
        }

        public void setCreationDate(String creationDate) {
            this.creationDate = creationDate;
        }

        public String getProduct() {
            return product;
        }

        public void setProduct(String product) {
            this.product = product;
        }

        public String getRomName() {
            return romName;
        }

        public void setRomName(String romName) {
            this.romName = romName;
        }

        public int getRomVersionMajor() {
            return romVersionMajor;
        }

        public void setRomVersionMajor(int romVersionMajor) {
            this.romVersionMajor = romVersionMajor;
        }

        public int getRomVersionMinor() {
            return romVersionMinor;
        }

        public void setRomVersionMinor(int romVersionMinor) {
            this.romVersionMinor = romVersionMinor;
        }

        public String getTargetEngine() {
            return targetEngine;
        }

        public void setTargetEngine(String targetEngine) {
            this.targetEngine = targetEngine;
        }

        public String getTools() {
            return tools;
        }

        public void setTools(String tools) {
            this.tools = tools;
        }

        public void addResource(SoundbankResource resource) {
            if (resource is SF2Instrument)
                instruments.Add((SF2Instrument)resource);
            if (resource is SF2Layer)
                layers.Add((SF2Layer)resource);
            if (resource is SF2Sample)
                samples.Add((SF2Sample)resource);
        }

        public void removeResource(SoundbankResource resource) {
            if (resource is SF2Instrument)
                instruments.Remove((SF2Instrument)resource);
            if (resource is SF2Layer)
                layers.Remove((SF2Layer)resource);
            if (resource is SF2Sample)
                samples.Remove((SF2Sample)resource);
        }

        public void addInstrument(SF2Instrument resource) {
            instruments.Add(resource);
        }

        public void removeInstrument(SF2Instrument resource) {
            instruments.Remove(resource);
        }

    }
}
