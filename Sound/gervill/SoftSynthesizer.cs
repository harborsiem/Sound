/*
 * Copyright (c) 2008, 2024, Oracle and/or its affiliates. All rights reserved.
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

//import sun.awt.OSInfo;

//import java.io.BufferedInputStream;
//import java.io.File;
//import java.io.FileInputStream;
//import java.io.FileNotFoundException;
//import java.io.FileOutputStream;
//import java.io.IOException;
//import java.io.InputStream;
//import java.io.OutputStream;
//import java.lang.ref.WeakReference;
//import java.util.ArrayList;
//import java.util.Arrays;
//import java.util.HashMap;
//import java.util.List;
//import java.util.Map;
//import java.util.Properties;
//import java.util.StringTokenizer;
//import java.util.prefs.BackingStoreException;
//import java.util.prefs.Preferences;

//import javax.sound.midi.Instrument;
//import javax.sound.midi.MidiChannel;
//import javax.sound.midi.MidiDevice;
//import javax.sound.midi.MidiSystem;
//import javax.sound.midi.MidiUnavailableException;
//import javax.sound.midi.Patch;
//import javax.sound.midi.Receiver;
//import javax.sound.midi.Soundbank;
//import javax.sound.midi.Transmitter;
//import javax.sound.midi.VoiceStatus;
//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.LineUnavailableException;
//import javax.sound.sampled.SourceDataLine;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Security;
using System.Globalization;
using SystemX.Sound.Midi;
using SystemX.Sound.Sampled;
using SystemX.Addon;

namespace SystemX.Media.Sound {

    /**
     * The software synthesizer class.
     *
     * @author Karl Helgason
     */
    public sealed class SoftSynthesizer : IAudioSynthesizer,
            IReferenceCountingDevice {

        internal sealed class WeakAudioStream : InputStream {
            private volatile AudioInputStream stream;
            public SoftAudioPusher pusher = null;
            public AudioInputStream jitter_stream = null;
            public ISourceDataLine sourceDataLine = null;
            public long silent_samples; //a@ volatile 
            private int framesize = 0;
            private readonly WeakReference weak_stream_link; //<AudioInputStream>
            private readonly AudioFloatConverter converter;
            private float[] silentbuffer = null;
            private readonly int samplesize;

            public override long Position {
                get { return stream.Position; }
            }

            public override long Length {
                get { return stream.Length; }
            }

            public void setInputStream(AudioInputStream stream) {
                this.stream = stream;
            }

            public override int available() {
                AudioInputStream local_stream = stream;
                if (local_stream != null)
                    return (int)local_stream.available();
                return 0;
            }

            public override int ReadByte() {
                byte[] b = new byte[1];
                if (Read(b, 0, b.Length) <= 0)
                    return -1;
                return b[0] & 0xFF;
            }

            public override int Read(byte[] buffer, int offset, int count) {
                AudioInputStream local_stream = stream;
                if (local_stream != null)
                    return local_stream.Read(buffer, offset, count);
                else {
                    int flen = count / samplesize;
                    if (silentbuffer == null || silentbuffer.Length < flen)
                        silentbuffer = new float[flen];
                    converter.toByteArray(silentbuffer, flen, buffer, offset);

                    Interlocked.Add(ref silent_samples, (long)((count / framesize)));

                    RunnableImpl runnable = new RunnableImpl(this);

                    if (pusher != null)
                        if (weak_stream_link.Target == null) {
                            pusher = null;
                            jitter_stream = null;
                            sourceDataLine = null;
                            Thread thread = new Thread(runnable.run);
                            thread.Name = "Synthesizer";
                            thread.Start();
                        }
                    return count;
                }
            }

            private class RunnableImpl {
                SoftAudioPusher _pusher; // = pusher;
                AudioInputStream _jitter_stream; // = jitter_stream;
                ISourceDataLine _sourceDataLine; // = sourceDataLine;
                public RunnableImpl(WeakAudioStream caller) {
                    _pusher = caller.pusher;
                    _jitter_stream = caller.jitter_stream;
                    _sourceDataLine = caller.sourceDataLine;
                }

                public void run() {
                    _pusher.stop();
                    if (_jitter_stream != null)
                        try {
                            _jitter_stream.Close();
                        } catch (IOException e) {
                            Printer.printStackTrace(e);
                        }
                    if (_sourceDataLine != null)
                        _sourceDataLine.close();
                }
            }

            public WeakAudioStream(AudioInputStream stream) {
                this.stream = stream;
                weak_stream_link = new WeakReference(stream);
                converter = AudioFloatConverter.getConverter(stream.getFormat());
                samplesize = stream.getFormat().getFrameSize() / stream.getFormat().getChannels();
                framesize = stream.getFormat().getFrameSize();
            }

            public AudioInputStream getAudioInputStream() {
                return new AudioInputStream(this, stream.getFormat(), AudioSystem.NOT_SPECIFIED);
            }

            public override void Close() {
                AudioInputStream astream = (AudioInputStream)weak_stream_link.Target;
                if (astream != null)
                    astream.Close();
            }

            public long ReadSilentSamples() {
                return Interlocked.Read(ref silent_samples);
            }

            public void WriteSilentSamples(long value) {
                Interlocked.Exchange(ref silent_samples, value);
            }
        }

        private class Info : MidiDevice.Info {
            internal Info()
                : base(INFO_NAME, INFO_VENDOR, INFO_DESCRIPTION, INFO_VERSION) {
            }
        }

        internal const String INFO_NAME = "Gervill";
        internal const String INFO_VENDOR = "OpenJDK";
        internal const String INFO_DESCRIPTION = "Software MIDI Synthesizer";
        internal const String INFO_VERSION = "1.0";
        internal static readonly MidiDevice.Info info = new Info();

        private static ISourceDataLine testline = null;

        private static ISoundbank defaultSoundBank = null;

        internal WeakAudioStream weakstream = null;

        internal readonly Object control_mutex; // = this;

        internal int voiceIDCounter = 0;

        // 0: default
        // 1: DLS Voice Allocation
        internal int voice_allocation_mode = 0;

        internal bool load_default_soundbank = false;
        internal bool reverb_light = true;
        internal bool reverb_on = true;
        internal bool chorus_on = true;
        internal bool agc_on = true;

        internal SoftChannel[] channels;
        internal SoftChannelProxy[] external_channels = null;

        private bool largemode = false;

        // 0: GM Mode off (default)
        // 1: GM Level 1
        // 2: GM Level 2
        private int gmmode = 0;

        private int deviceid = 0;

        private AudioFormat format = new AudioFormat(44100, 16, 2, true, false);

        private ISourceDataLine sourceDataLine = null;

        private SoftAudioPusher pusher = null;
        private AudioInputStream pusher_stream = null;

        private float controlrate = 147f;

        private bool _open = false;
        private bool implicitOpen = false;

        private String resamplerType = "linear";
        private ISoftResampler resampler = new SoftLinearResampler();

        private int number_of_midi_channels = 16;
        private int maxpoly = 64;
        private long latency = 200000; // 200 msec
        private bool jitter_correction = false;

        private SoftMainMixer mainmixer;
        private SoftVoice[] voices;

        private readonly Dictionary<String, SoftTuning> tunings
                = new Dictionary<String, SoftTuning>();
        private readonly Dictionary<String, SoftInstrument> inslist
                = new Dictionary<String, SoftInstrument>();
        private readonly Dictionary<String, ModelInstrument> loadedlist
                = new Dictionary<String, ModelInstrument>();

        private readonly List<IReceiver> recvslist = new List<IReceiver>();

        public SoftSynthesizer() {
            control_mutex = this;
        }

        private void getBuffers(ModelInstrument instrument,
                IList<ModelByteBuffer> buffers) {
            foreach (ModelPerformer performer in instrument.getPerformers()) {
                if (performer.getOscillators() != null) {
                    foreach (IModelOscillator osc in performer.getOscillators()) {
                        if (osc is ModelByteBufferWavetable) {
                            ModelByteBufferWavetable w = (ModelByteBufferWavetable)osc;
                            ModelByteBuffer buff = w.getBuffer();
                            if (buff != null)
                                buffers.Add(buff);
                            buff = w.get8BitExtensionBuffer();
                            if (buff != null)
                                buffers.Add(buff);
                        }
                    }
                }
            }
        }

        private bool loadSamples(IList<ModelInstrument> instruments) {
            if (largemode)
                return true;
            List<ModelByteBuffer> buffers = new List<ModelByteBuffer>();
            foreach (ModelInstrument instrument in instruments)
                getBuffers(instrument, buffers);
            try {
                ModelByteBuffer.loadAll(buffers);
            } catch (IOException) {
                return false;
            }
            return true;
        }

        private bool loadInstruments(List<ModelInstrument> instruments) {
            if (!isOpen())
                return false;
            if (!loadSamples(instruments))
                return false;

            lock (control_mutex) {
                if (channels != null)
                    foreach (SoftChannel c in channels) {
                        c.current_instrument = null;
                        c.current_director = null;
                    }
                foreach (ModelInstrument instrument in instruments) {
                    String pat = patchToString(instrument.getPatch());
                    SoftInstrument softins
                            = new SoftInstrument(instrument);
                    inslist[pat] = softins;
                    loadedlist[pat] = instrument;
                }
            }

            return true;
        }

        private void processPropertyInfo(IDictionary<String, Object> info) {
            AudioSynthesizerPropertyInfo[] items = getPropertyInfo(info);

            String resamplerType = (String)items[0].value;
            if (resamplerType.Equals("point", StringComparison.OrdinalIgnoreCase)) {
                this.resampler = new SoftPointResampler();
                this.resamplerType = "point";
            } else if (resamplerType.Equals("linear", StringComparison.OrdinalIgnoreCase)) {
                this.resampler = new SoftLinearResampler2();
                this.resamplerType = "linear";
            } else if (resamplerType.Equals("linear1", StringComparison.OrdinalIgnoreCase)) {
                this.resampler = new SoftLinearResampler();
                this.resamplerType = "linear1";
            } else if (resamplerType.Equals("linear2", StringComparison.OrdinalIgnoreCase)) {
                this.resampler = new SoftLinearResampler2();
                this.resamplerType = "linear2";
            } else if (resamplerType.Equals("cubic", StringComparison.OrdinalIgnoreCase)) {
                this.resampler = new SoftCubicResampler();
                this.resamplerType = "cubic";
            } else if (resamplerType.Equals("lanczos", StringComparison.OrdinalIgnoreCase)) {
                this.resampler = new SoftLanczosResampler();
                this.resamplerType = "lanczos";
            } else if (resamplerType.Equals("sinc", StringComparison.OrdinalIgnoreCase)) {
                this.resampler = new SoftSincResampler();
                this.resamplerType = "sinc";
            }

            setFormat((AudioFormat)items[2].value);
            controlrate = (float)items[1].value;
            latency = (long)items[3].value;
            deviceid = (int)items[4].value;
            maxpoly = (int)items[5].value;
            reverb_on = (Boolean)items[6].value;
            chorus_on = (Boolean)items[7].value;
            agc_on = (Boolean)items[8].value;
            largemode = (Boolean)items[9].value;
            number_of_midi_channels = (int)items[10].value;
            jitter_correction = (Boolean)items[11].value;
            reverb_light = (Boolean)items[12].value;
            load_default_soundbank = (Boolean)items[13].value;
        }

        private String patchToString(Patch patch) {
            if (patch is ModelPatch && ((ModelPatch)patch).isPercussion())
                return "p." + patch.getProgram() + "." + patch.getBank();
            else
                return patch.getProgram() + "." + patch.getBank();
        }

        private void setFormat(AudioFormat format) {
            if (format.getChannels() > 2) {
                throw new ArgumentException(
                        "Only mono and stereo audio supported.");
            }
            if (AudioFloatConverter.getConverter(format) == null)
                throw new ArgumentException("Audio format not supported.");
            this.format = format;
        }

        internal void removeReceiver(IReceiver recv) {
            bool perform_close = false;
            lock (control_mutex) {
                if (recvslist.Remove(recv)) {
                    if (implicitOpen && recvslist.Count == 0)
                        perform_close = true;
                }
            }
            if (perform_close)
                close();
        }

        internal SoftMainMixer getMainMixer() {
            if (!isOpen())
                return null;
            return mainmixer;
        }

        internal SoftInstrument findInstrument(int program, int bank, int channel) {

            SoftInstrument current_instrument;
            String p_plaf;
            // Add support for GM2 banks 0x78 and 0x79
            // as specified in DLS 2.2 in Section 1.4.6
            // which allows using percussion and melodic instruments
            // on all channels
            if (bank >> 7 == 0x78 || bank >> 7 == 0x79) {
                current_instrument = null;
                if (inslist.ContainsKey(program + "." + bank)) {
                    current_instrument = inslist[program + "." + bank];
                }
                if (current_instrument != null)
                    return current_instrument;

                if (bank >> 7 == 0x78)
                    p_plaf = "p.";
                else
                    p_plaf = "";

                // Instrument not found fallback to MSB:bank, LSB:0
                current_instrument = null;
                if (inslist.ContainsKey(p_plaf + program + "."
                            + ((bank & 128) << 7))) {
                    current_instrument = inslist[p_plaf + program + "."
                            + ((bank & 128) << 7)];
                }
                if (current_instrument != null)
                    return current_instrument;
                // Instrument not found fallback to MSB:0, LSB:bank
                current_instrument = null;
                if (inslist.ContainsKey(p_plaf + program + "."
                            + (bank & 128))) {
                    current_instrument = inslist[p_plaf + program + "."
                            + (bank & 128)];
                }
                if (current_instrument != null)
                    return current_instrument;
                // Instrument not found fallback to MSB:0, LSB:0
                current_instrument = null;
                if (inslist.ContainsKey(p_plaf + program + ".0")) {
                    current_instrument = inslist[p_plaf + program + ".0"];
                }
                if (current_instrument != null)
                    return current_instrument;
                // Instrument not found fallback to MSB:0, LSB:0, program=0
                current_instrument = null;
                if (inslist.ContainsKey(p_plaf + program + "0.0")) {
                    current_instrument = inslist[p_plaf + program + "0.0"];
                }
                if (current_instrument != null)
                    return current_instrument;
                return null;
            }

            // Channel 10 uses percussion instruments
            //String p_plaf;
            if (channel == 9)
                p_plaf = "p.";
            else
                p_plaf = "";

            current_instrument = null;
            if (inslist.ContainsKey(p_plaf + program + "." + bank)) {
                current_instrument
                        = inslist[p_plaf + program + "." + bank];
            }
            if (current_instrument != null)
                return current_instrument;
            // Instrument not found fallback to MSB:0, LSB:0
            current_instrument = null;
            if (inslist.ContainsKey(p_plaf + program + ".0")) {
                current_instrument = inslist[p_plaf + program + ".0"];
            }
            if (current_instrument != null)
                return current_instrument;
            // Instrument not found fallback to MSB:0, LSB:0, program=0
            current_instrument = null;
            if (inslist.ContainsKey(p_plaf + "0.0")) {
                current_instrument = inslist[p_plaf + "0.0"];
            }
            if (current_instrument != null)
                return current_instrument;
            return null;
        }

        internal int getVoiceAllocationMode() {
            return voice_allocation_mode;
        }

        internal int getGeneralMidiMode() {
            return gmmode;
        }

        internal void setGeneralMidiMode(int gmmode) {
            this.gmmode = gmmode;
        }

        internal int getDeviceID() {
            return deviceid;
        }

        internal float getControlRate() {
            return controlrate;
        }

        internal SoftVoice[] getVoices() {
            return voices;
        }

        internal SoftTuning getTuning(Patch patch) {
            String t_id = patchToString(patch);
            SoftTuning tuning = null;
            if (tunings.ContainsKey(t_id)) {
                tuning = tunings[t_id];
            }
            if (tuning == null) {
                tuning = new SoftTuning(patch);
                tunings[t_id] = tuning;
            }
            return tuning;
        }

        public long getLatency() {
            lock (control_mutex) {
                return latency;
            }
        }

        public AudioFormat getFormat() {
            lock (control_mutex) {
                return format;
            }
        }

        public int getMaxPolyphony() {
            lock (control_mutex) {
                return maxpoly;
            }
        }

        public IMidiChannel[] getChannels() {

            lock (control_mutex) {
                // if (external_channels == null) => the synthesizer is not open,
                // create 16 proxy channels
                // otherwise external_channels has the same length as channels array
                if (external_channels == null) {
                    external_channels = new SoftChannelProxy[16];
                    for (int i = 0; i < external_channels.Length; i++)
                        external_channels[i] = new SoftChannelProxy();
                }
                IMidiChannel[] ret;
                if (isOpen())
                    ret = new IMidiChannel[channels.Length];
                else
                    ret = new IMidiChannel[16];
                for (int i = 0; i < ret.Length; i++)
                    ret[i] = external_channels[i];
                return ret;
            }
        }

        public VoiceStatus[] getVoiceStatus() {
            if (!isOpen()) {
                VoiceStatus[] tempVoiceStatusArray
                        = new VoiceStatus[getMaxPolyphony()];
                for (int i = 0; i < tempVoiceStatusArray.Length; i++) {
                    VoiceStatus b = new VoiceStatus();
                    b.active = false;
                    b.bank = 0;
                    b.channel = 0;
                    b.note = 0;
                    b.program = 0;
                    b.volume = 0;
                    tempVoiceStatusArray[i] = b;
                }
                return tempVoiceStatusArray;
            }

            lock (control_mutex) {
                VoiceStatus[] tempVoiceStatusArray = new VoiceStatus[voices.Length];
                for (int i = 0; i < voices.Length; i++) {
                    VoiceStatus a = voices[i];
                    VoiceStatus b = new VoiceStatus();
                    b.active = a.active;
                    b.bank = a.bank;
                    b.channel = a.channel;
                    b.note = a.note;
                    b.program = a.program;
                    b.volume = a.volume;
                    tempVoiceStatusArray[i] = b;
                }
                return tempVoiceStatusArray;
            }
        }

        public bool isSoundbankSupported(ISoundbank soundbank) {
            foreach (Instrument ins in soundbank.getInstruments())
                if (!(ins is ModelInstrument))
                    return false;
            return true;
        }

        public bool loadInstrument(Instrument instrument) {
            if (!(instrument is ModelInstrument modelInstrument)) {
                throw new ArgumentException("Unsupported instrument: " +
                        instrument);
            }
            List<ModelInstrument> instruments = new List<ModelInstrument>();
            instruments.Add(modelInstrument);
            return loadInstruments(instruments);
        }

        public void unloadInstrument(Instrument instrument) {
            if (!(instrument is ModelInstrument modelInstrument)) {
                throw new ArgumentException("Unsupported instrument: " +
                        instrument);
            }
            if (!isOpen())
                return;

            String pat = patchToString(modelInstrument.getPatch());
            lock (control_mutex) {
                foreach (SoftChannel c in channels)
                    c.current_instrument = null;
                inslist.Remove(pat);
                loadedlist.Remove(pat);
                for (int i = 0; i < channels.Length; i++) {
                    channels[i].allSoundOff();
                }
            }
        }

        public bool remapInstrument(Instrument from, Instrument to) {

            if (from == null)
                throw new ArgumentNullException(nameof(from));
            if (to == null)
                throw new ArgumentNullException(nameof(to));
            if (!(from is ModelInstrument)) {
                throw new ArgumentException("Unsupported instrument: " +
                        from.ToString());
            }
            if (!(to is ModelInstrument)) {
                throw new ArgumentException("Unsupported instrument: " +
                        to.ToString());
            }
            if (!isOpen())
                return false;

            lock (control_mutex) {
                if (!loadedlist.ContainsValue((ModelInstrument)to))
                    throw new ArgumentException("Instrument to is not loaded.");
                unloadInstrument(from);
                ModelMappedInstrument mfrom = new ModelMappedInstrument(
                        (ModelInstrument)to, from.getPatch());
                return loadInstrument(mfrom);
            }
        }

        public ISoundbank getDefaultSoundbank() { //a@
            lock (typeof(SoftSynthesizer)) {
                if (defaultSoundBank != null)
                    return defaultSoundBank;
                try {
                    String dotNetHome = Service.GetHome();
                    DirectoryInfo libaudio = new DirectoryInfo(Path.Combine(dotNetHome, "Sound"));

                    if (libaudio.Exists) {
                        FileInfo foundfile = null;
                        FileSystemInfo[] files = libaudio.GetFileSystemInfos();
                        if (files != null) {
                            for (int i = 0; i < files.Length; i++) {
                                FileSystemInfo file = files[i];
                                if (file is FileInfo) {
                                    FileInfo file0 = (FileInfo)file;
                                    String lname = file0.Name.ToUpperInvariant();
                                    if (lname.EndsWith(".SF2", StringComparison.Ordinal)
                                            || lname.EndsWith(".DLS", StringComparison.Ordinal)) {
                                        if (foundfile == null
                                            || (file0.Length > foundfile.Length)) {
                                            foundfile = file0;
                                        }
                                    }
                                }
                            }
                        }
                        if (foundfile != null) {
                            try {
                                ISoundbank sbk = MidiSystem.getSoundbank(foundfile);
                                defaultSoundBank = sbk;
                                return defaultSoundBank;
                            } catch (Exception) {
                                //e.printStackTrace();
                            }
                        }
                    }

                    //if (Environment.OSVersion.Platform == PlatformID.Unix) //LINUX
                    //         {

                    //    FileInfo[] systemSoundFontsDir = new FileInfo[] {
                    //        /* Arch, Fedora, Mageia */
                    //        new FileInfo("/usr/share/soundfonts/"),
                    //        new FileInfo("/usr/local/share/soundfonts/"),
                    //        /* Debian, Gentoo, OpenSUSE, Ubuntu */
                    //        new FileInfo("/usr/share/sounds/sf2/"),
                    //        new FileInfo("/usr/local/share/sounds/sf2/"),
                    //    };

                    //    /*
                    //     * Look for a default.sf2
                    //     */
                    //    foreach (FileInfo systemSoundFontDir in systemSoundFontsDir) {
                    //        if (systemSoundFontDir.Exists) {
                    //            FileInfo defaultSoundFont = new FileInfo(Path.Combine(systemSoundFontDir.FullName, "default.sf2"));
                    //            if (defaultSoundFont.Exists) {
                    //                try {
                    //                    return MidiSystem.getSoundbank(defaultSoundFont);
                    //                }
                    //                catch (IOException e) {
                    //                    // continue with lookup
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    if (Environment.OSVersion.Platform == PlatformID.Win32NT) {
                        FileInfo gm_dls = new FileInfo(Environment.GetFolderPath(Environment.SpecialFolder.System)
                                + "\\drivers\\gm.dls"); //a@ system32\drivers
                        if (gm_dls.Exists) {
                            try {
                                ISoundbank sbk = MidiSystem.getSoundbank(gm_dls);
                                defaultSoundBank = sbk;
                                return defaultSoundBank;
                            } catch (Exception) {
                                //e.printStackTrace();
                            }
                        }
                    }
                } catch (SecurityException) { //AccessControlException
                } catch (Exception) {
                    //e.printStackTrace();
                }

                DirectoryInfo userhome = null;
                FileInfo emg_soundbank_file = null;

                /*
                 *  Try to load saved generated soundbank 
                 */
                try {
                    userhome = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Gervill"));
                    emg_soundbank_file = new FileInfo(Path.Combine(userhome.FullName, "soundbank-emg.sf2"));
                    if (userhome.Exists && emg_soundbank_file.Exists) {
                        ISoundbank sbk = MidiSystem.getSoundbank(emg_soundbank_file);
                        defaultSoundBank = sbk;
                        return defaultSoundBank;
                    }
                } catch (SecurityException) {
                } catch (Exception) {
                    //e.printStackTrace();
                }

                try {

                    /*
                     *  Generate emergency soundbank 
                     */
                    defaultSoundBank = EmergencySoundbank.createSoundbank();

                    /*
                     *  Save generated soundbank to disk for faster future use. 
                     */
                    if (defaultSoundBank != null) {
                        if (!userhome.Exists) {
                            Directory.CreateDirectory(userhome.FullName);
                        }
                        if (!emg_soundbank_file.Exists) {
                            ((SF2Soundbank)defaultSoundBank).save(emg_soundbank_file);
                        }
                    }
                } catch (Exception) {
                    //e.printStackTrace();
                }
            }
            return defaultSoundBank;
        }

        public Instrument[] getAvailableInstruments() {
            ISoundbank defsbk = getDefaultSoundbank();
            if (defsbk == null)
                return new Instrument[0];
            Instrument[] inslist_array = defsbk.getInstruments();
            Array.Sort(inslist_array, new ModelInstrumentComparator());
            return inslist_array;
        }

        public Instrument[] getLoadedInstruments() {
            if (!isOpen())
                return new Instrument[0];

            lock (control_mutex) {
                ModelInstrument[] inslist_array =
                        new ModelInstrument[loadedlist.Values.Count];
                loadedlist.Values.CopyTo(inslist_array, 0);
                Array.Sort(inslist_array, new ModelInstrumentComparator());
                return inslist_array;
            }
        }

        public bool loadAllInstruments(ISoundbank soundbank) {
            List<ModelInstrument> instruments = new List<ModelInstrument>();
            foreach (Instrument ins in soundbank.getInstruments()) {
                if (!(ins is ModelInstrument modelInstrument)) {
                    throw new ArgumentException(
                            "Unsupported instrument: " + ins);
                }
                instruments.Add(modelInstrument);
            }
            return loadInstruments(instruments);
        }

        public void unloadAllInstruments(ISoundbank soundbank) {
            if (soundbank == null || !isSoundbankSupported(soundbank))
                throw new ArgumentException("Unsupported soundbank: " + soundbank);

            if (!isOpen())
                return;

            foreach (Instrument ins in soundbank.getInstruments()) {
                if (ins is ModelInstrument) {
                    unloadInstrument(ins);
                }
            }
        }

        public bool loadInstruments(ISoundbank soundbank, Patch[] patchList) {
            List<ModelInstrument> instruments = new List<ModelInstrument>();
            foreach (Patch patch in patchList) {
                Instrument ins = soundbank.getInstrument(patch);
                if (!(ins is ModelInstrument modelInstrument)) {
                    throw new ArgumentException(
                            "Unsupported instrument: " + ins);
                }
                instruments.Add(modelInstrument);
            }
            return loadInstruments(instruments);
        }

        public void unloadInstruments(ISoundbank soundbank, Patch[] patchList) {
            if (soundbank == null || !isSoundbankSupported(soundbank))
                throw new ArgumentException("Unsupported soundbank: " + soundbank);

            if (!isOpen())
                return;

            foreach (Patch pat in patchList) {
                Instrument ins = soundbank.getInstrument(pat);
                if (ins is ModelInstrument) {
                    unloadInstrument(ins);
                }
            }
        }

        public MidiDevice.Info getDeviceInfo() {
            return info;
        }

        private IDictionary<String, String> getStoredProperties() { //@
            return new Dictionary<String, String>();
            //return AccessController
            //           .doPrivileged((PrivilegedAction<Properties>) () -> {
            //                Properties p = new Properties();
            //                String notePath = "/com/sun/media/sound/softsynthesizer";
            //                try {
            //                    Preferences prefroot = Preferences.userRoot();
            //                    if (prefroot.nodeExists(notePath)) {
            //                        Preferences prefs = prefroot.node(notePath);
            //                        String[] prefs_keys = prefs.keys();
            //                        foreach (String prefs_key in prefs_keys) {
            //                            String val = prefs.get(prefs_key, null);
            //                            if (val != null) {
            //                                p.setProperty(prefs_key, val);
            //                            }
            //                        }
            //                    }
            //                } catch (BackingStoreException ignored) {
            //                }
            //                return p;
            //        });
        }

        public AudioSynthesizerPropertyInfo[] getPropertyInfo(IDictionary<String, Object> info) { //@
            List<AudioSynthesizerPropertyInfo> list = new List<AudioSynthesizerPropertyInfo>();

            AudioSynthesizerPropertyInfo item;

            // If info != null or synthesizer is closed
            //   we return how the synthesizer will be set on next open
            // If info == null and synthesizer is open
            //   we return current synthesizer properties.
            bool o = info == null && _open;

            item = new AudioSynthesizerPropertyInfo("interpolation", o ? resamplerType : "linear");
            item.choices = new String[]{"linear", "linear1", "linear2", "cubic",
                                    "lanczos", "sinc", "point"};
            item.description = "Interpolation method";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("control rate", o ? controlrate : 147f);
            item.description = "Control rate";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("format",
                    o ? format : new AudioFormat(44100, 16, 2, true, false));
            item.description = "Default audio format";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("latency", o ? latency : 120000L);
            item.description = "Default latency";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("device id", o ? deviceid : 0);
            item.description = "Device ID for SysEx Messages";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("max polyphony", o ? maxpoly : 64);
            item.description = "Maximum polyphony";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("reverb", o ? reverb_on : true);
            item.description = "Turn reverb effect on or off";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("chorus", o ? chorus_on : true);
            item.description = "Turn chorus effect on or off";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("auto gain control", o ? agc_on : true);
            item.description = "Turn auto gain control on or off";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("large mode", o ? largemode : false);
            item.description = "Turn large mode on or off.";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("midi channels", o ? channels.Length : 16);
            item.description = "Number of midi channels.";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("jitter correction", o ? jitter_correction : true);
            item.description = "Turn jitter correction on or off.";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("light reverb", o ? reverb_light : true);
            item.description = "Turn light reverb mode on or off";
            list.Add(item);

            item = new AudioSynthesizerPropertyInfo("load default soundbank", o ? load_default_soundbank : true);
            item.description = "Enabled/disable loading default soundbank";
            list.Add(item);

            AudioSynthesizerPropertyInfo[] items;
            items = list.ToArray();

            IDictionary<String, String> storedProperties = getStoredProperties();

            if (info != null)
                foreach (AudioSynthesizerPropertyInfo item2 in items) {
                    Object v = null;
                    if (info.ContainsKey(item2.name)) {
                        v = info[item2.name];
                    }
                    String propsValue = null;
                    if (storedProperties.ContainsKey(item2.name)) {
                        propsValue = storedProperties[item2.name];
                    }
                    v = (v != null) ? v : propsValue;
                    if (v != null) {
                        Type c = (item2.valueClass);
                        if (c.IsInstanceOfType(v))
                            item2.value = v;
                        else if (v is String) {
                            String s = (String)v;
                            if (c == typeof(Boolean)) {
                                if (s.Equals("true", StringComparison.OrdinalIgnoreCase))
                                    item2.value = (Boolean)true;
                                if (s.Equals("false", StringComparison.OrdinalIgnoreCase))
                                    item2.value = (Boolean)false;
                            } else if (c == typeof(AudioFormat)) {
                                int channels0 = 2;
                                bool signed = true;
                                bool bigendian = false;
                                int bits = 16;
                                float sampleRate = 44100f;
                                int index = 0;
                                try {
                                    String[] st = s.Split(new String[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                                    String prevToken = "";
                                    while (st.Length > index) {
                                        String token = st[index++].ToLowerInvariant();
                                        if (token.Equals("mono"))
                                            channels0 = 1;
                                        if (token.StartsWith("channel", StringComparison.Ordinal))
                                            channels0 = Int32.Parse(prevToken, NumberFormatInfo.InvariantInfo);
                                        if (token.Contains("unsigned"))
                                            signed = false;
                                        if (token.Equals("big-endian"))
                                            bigendian = true;
                                        if (token.Equals("bit"))
                                            bits = Int32.Parse(prevToken, NumberFormatInfo.InvariantInfo);
                                        if (token.Equals("hz"))
                                            sampleRate = Single.Parse(prevToken, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                                        prevToken = token;
                                    }
                                    item2.value = new AudioFormat(sampleRate, bits,
                                            channels0, signed, bigendian);
                                } catch (FormatException) {
                                }

                            } else
                                try {
                                    if (c == typeof(Byte))
                                        item2.value = Byte.Parse(s, NumberFormatInfo.InvariantInfo);
                                    else if (c == typeof(Int16))
                                        item2.value = Int16.Parse(s, NumberFormatInfo.InvariantInfo);
                                    else if (c == typeof(Int32))
                                        item2.value = Int32.Parse(s, NumberFormatInfo.InvariantInfo);
                                    else if (c == typeof(Int64))
                                        item2.value = Int64.Parse(s, NumberFormatInfo.InvariantInfo);
                                    else if (c == typeof(Single))
                                        item2.value = Single.Parse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                                    else if (c == typeof(Double))
                                        item2.value = Double.Parse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo);
                                } catch (FormatException) {
                                }
                        } else if (v is ValueType) {
                            ValueType n = (ValueType)v;
                            if (c == typeof(Byte))
                                item2.value = (Byte)n;
                            if (c == typeof(Int16))
                                item2.value = (Int16)n;
                            if (c == typeof(Int32))
                                item2.value = (Int32)n;
                            if (c == typeof(Int64))
                                item2.value = (Int64)n;
                            if (c == typeof(Single))
                                item2.value = (Single)n;
                            if (c == typeof(Double))
                                item2.value = (Double)n;
                        }
                    }
                }

            return items;
        }

        public void open() {
            if (isOpen()) {
                lock (control_mutex) {
                    implicitOpen = false;
                }
                return;
            }
            open(null, null);
        }

        public void open(ISourceDataLine line, IDictionary<String, Object> info) {
            if (isOpen()) {
                lock (control_mutex) {
                    implicitOpen = false;
                }
                return;
            }
            lock (control_mutex) {
                Exception causeException = null;
                try {
                    if (line != null) {
                        // can throw IllegalArgumentException
                        setFormat(line.getFormat());
                    }

                    AudioInputStream ais = openStream(getFormat(), info);

                    weakstream = new WeakAudioStream(ais);
                    ais = weakstream.getAudioInputStream();

                    if (line == null) {
                        if (testline != null) {
                            line = testline;
                        } else {
                            // can throw LineUnavailableException,
                            // IllegalArgumentException
                            line = AudioSystem.getSourceDataLine(getFormat());
                        }
                    }

                    double latency = this.latency;

                    if (!line.isOpen()) {
                        int bufferSize = getFormat().getFrameSize()
                            * (int)(getFormat().getFrameRate() * (latency / 1000000f));
                        // can throw LineUnavailableException,
                        // IllegalArgumentException
                        line.open(getFormat(), bufferSize);

                        // Remember that we opened that line
                        // so we can close again in SoftSynthesizer.close()
                        sourceDataLine = line;
                    }
                    if (!line.isActive())
                        line.start();

                    int controlbuffersize = 512;
                    try {
                        controlbuffersize = (int)ais.available();
                    } catch (IOException) {
                    }

                    // Tell mixer not fill read buffers fully.
                    // This lowers latency, and tells DataPusher
                    // to read in smaller amounts.
                    //mainmixer.readfully = false;
                    //pusher = new DataPusher(line, ais);

                    int buffersize = line.getBufferSize();
                    buffersize -= buffersize % controlbuffersize;

                    if (buffersize < 3 * controlbuffersize)
                        buffersize = 3 * controlbuffersize;

                    if (jitter_correction) {
                        ais = new SoftJitterCorrector(ais, buffersize,
                                controlbuffersize);
                        if (weakstream != null)
                            weakstream.jitter_stream = ais;
                    }
                    pusher = new SoftAudioPusher(line, ais, controlbuffersize);
                    pusher_stream = ais;
                    pusher.start();

                    if (weakstream != null) {
                        weakstream.pusher = pusher;
                        weakstream.sourceDataLine = sourceDataLine;
                    }

                }
                //@@@ .NET implementation
                catch (LineUnavailableException e) {
                    causeException = e;
                } catch (ArgumentException e) {
                    causeException = e;
                }

                if (causeException != null) {
                    if (isOpen()) {
                        close();
                    }
                    // am: need MidiUnavailableException(Throwable) ctor!
                    MidiUnavailableException ex = new MidiUnavailableException(
                            "Can not open line", causeException);
                    throw ex;
                }
            }
        }

        public AudioInputStream openStream(AudioFormat targetFormat,
                                           IDictionary<String, Object> info) {

            if (isOpen())
                throw new MidiUnavailableException("Synthesizer is already open");

            lock (control_mutex) {

                gmmode = 0;
                voice_allocation_mode = 0;

                processPropertyInfo(info);

                _open = true;
                implicitOpen = false;

                if (targetFormat != null)
                    setFormat(targetFormat);

                if (load_default_soundbank) {
                    ISoundbank defbank = getDefaultSoundbank();
                    if (defbank != null) {
                        loadAllInstruments(defbank);
                    }
                }

                voices = new SoftVoice[maxpoly];
                for (int i = 0; i < maxpoly; i++)
                    voices[i] = new SoftVoice(this);

                mainmixer = new SoftMainMixer(this);

                channels = new SoftChannel[number_of_midi_channels];
                for (int i = 0; i < channels.Length; i++)
                    channels[i] = new SoftChannel(this, i);

                if (external_channels == null) {
                    // Always create external_channels array
                    // with 16 or more channels
                    // so getChannels works correctly
                    // when the synthesizer is closed.
                    if (channels.Length < 16)
                        external_channels = new SoftChannelProxy[16];
                    else
                        external_channels = new SoftChannelProxy[channels.Length];
                    for (int i = 0; i < external_channels.Length; i++)
                        external_channels[i] = new SoftChannelProxy();
                } else {
                    // We must resize external_channels array
                    // but we must also copy the old SoftChannelProxy
                    // into the new one
                    if (channels.Length > external_channels.Length) {
                        SoftChannelProxy[] new_external_channels
                                = new SoftChannelProxy[channels.Length];
                        for (int i = 0; i < external_channels.Length; i++)
                            new_external_channels[i] = external_channels[i];
                        for (int i = external_channels.Length;
                                i < new_external_channels.Length; i++) {
                            new_external_channels[i] = new SoftChannelProxy();
                        }
                    }
                }

                for (int i = 0; i < channels.Length; i++)
                    external_channels[i].setChannel(channels[i]);

                foreach (SoftVoice voice in getVoices())
                    voice.resampler = resampler.openStreamer();

                foreach (IReceiver recv in getReceivers()) {
                    SoftReceiver srecv = ((SoftReceiver)recv);
                    srecv.open = _open;
                    srecv.mainmixer = mainmixer;
                    srecv.midimessages = mainmixer.midimessages;
                }

                return mainmixer.getInputStream();
            }
        }

        public void close() {

            if (!isOpen())
                return;

            SoftAudioPusher pusher_to_be_closed = null;
            AudioInputStream pusher_stream_to_be_closed = null;
            lock (control_mutex) {
                if (pusher != null) {
                    pusher_to_be_closed = pusher;
                    pusher_stream_to_be_closed = pusher_stream;
                    pusher = null;
                    pusher_stream = null;
                }
            }

            if (pusher_to_be_closed != null) {
                // Pusher must not be closed synchronized against control_mutex,
                // this may result in synchronized conflict between pusher
                // and current thread.
                pusher_to_be_closed.stop();

                try {
                    pusher_stream_to_be_closed.Close();
                } catch (IOException) {
                    //e.printStackTrace();
                }
            }

            lock (control_mutex) {

                if (mainmixer != null)
                    mainmixer.close();
                _open = false;
                implicitOpen = false;
                mainmixer = null;
                voices = null;
                channels = null;

                if (external_channels != null)
                    for (int i = 0; i < external_channels.Length; i++)
                        external_channels[i].setChannel(null);

                if (sourceDataLine != null) {
                    sourceDataLine.close();
                    sourceDataLine = null;
                }

                inslist.Clear();
                loadedlist.Clear();
                tunings.Clear();

                while (recvslist.Count != 0)
                    recvslist[recvslist.Count - 1].close();

            }
        }

        public void Dispose() {
            close();
        }

        public bool isOpen() {
            lock (control_mutex) {
                return _open;
            }
        }

        public long getMicrosecondPosition() {

            if (!isOpen())
                return 0;

            lock (control_mutex) {
                return mainmixer.getMicrosecondPosition();
            }
        }

        public int getMaxReceivers() {
            return -1;
        }

        public int getMaxTransmitters() {
            return 0;
        }

        public IReceiver getReceiver() {

            lock (control_mutex) {
                SoftReceiver receiver = new SoftReceiver(this);
                receiver.open = _open;
                recvslist.Add(receiver);
                return receiver;
            }
        }

        public IList<IReceiver> getReceivers() {

            lock (control_mutex) {
                List<IReceiver> recvs = new List<IReceiver>();
                recvs.AddRange(recvslist);
                return recvs;
            }
        }

        public ITransmitter getTransmitter() {

            throw new MidiUnavailableException("No transmitter available");
        }

        public IList<ITransmitter> getTransmitters() {

            return new List<ITransmitter>();
        }

        public IReceiver getReceiverReferenceCounting() {

            if (!isOpen()) {
                open();
                lock (control_mutex) {
                    implicitOpen = true;
                }
            }

            return getReceiver();
        }

        public ITransmitter getTransmitterReferenceCounting() {

            throw new MidiUnavailableException("No transmitter available");
        }
    }
}
