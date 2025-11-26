/*
 * Copyright (c) 2008, 2013, Oracle and/or its affiliates. All rights reserved.
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
//import java.util.ArrayList;
//import java.util.List;

//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioFormat.Encoding;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.Clip;
//import javax.sound.sampled.Control;
//import javax.sound.sampled.Control.Type;
//import javax.sound.sampled.DataLine;
//import javax.sound.sampled.Line;
//import javax.sound.sampled.LineEvent;
//import javax.sound.sampled.LineListener;
//import javax.sound.sampled.LineUnavailableException;
//import javax.sound.sampled.Mixer;
//import javax.sound.sampled.SourceDataLine;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * Software audio mixer.
 * 
 * @author Karl Helgason
 */
    public sealed class SoftMixingMixer : IMixer {

        private class Info : Mixer.Info {
            internal Info()
                : base(INFO_NAME, INFO_VENDOR, INFO_DESCRIPTION, INFO_VERSION) {
            }
        }

        internal const String INFO_NAME = "Gervill Sound Mixer";

        internal const String INFO_VENDOR = "OpenJDK Proposal";

        internal const String INFO_DESCRIPTION = "Software Sound Mixer";

        internal const String INFO_VERSION = "1.0";

        internal static readonly Mixer.Info info = new SoftMixingMixer.Info();

        internal readonly Object control_mutex; // = this;

        internal bool implicitOpen = false;

        private bool _open = false;

        private SoftMixingMainMixer mainmixer = null;

        private AudioFormat format = new AudioFormat(44100, 16, 2, true, false);

        private ISourceDataLine sourceDataLine = null;

        private SoftAudioPusher pusher = null;

        private AudioInputStream pusher_stream = null;

        private readonly float controlrate = 147f;

        private readonly long latency = 100000; // 100 msec

        private readonly bool jitter_correction = false;

        private readonly List<ILineListener> listeners = new List<ILineListener>();

        private readonly Line.Info[] sourceLineInfo;

        public SoftMixingMixer() {
            control_mutex = this;

            sourceLineInfo = new Line.Info[2];

            List<AudioFormat> formats = new List<AudioFormat>();
            for (int channels = 1; channels <= 2; channels++) {
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                        AudioSystem.NOT_SPECIFIED, 8, channels, channels,
                        AudioSystem.NOT_SPECIFIED, false));
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                        AudioSystem.NOT_SPECIFIED, 8, channels, channels,
                        AudioSystem.NOT_SPECIFIED, false));
                for (int bits = 16; bits < 32; bits += 8) {
                    formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                            AudioSystem.NOT_SPECIFIED, bits, channels, channels
                                    * bits / 8, AudioSystem.NOT_SPECIFIED, false));
                    formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                            AudioSystem.NOT_SPECIFIED, bits, channels, channels
                                    * bits / 8, AudioSystem.NOT_SPECIFIED, false));
                    formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                            AudioSystem.NOT_SPECIFIED, bits, channels, channels
                                    * bits / 8, AudioSystem.NOT_SPECIFIED, true));
                    formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                            AudioSystem.NOT_SPECIFIED, bits, channels, channels
                                    * bits / 8, AudioSystem.NOT_SPECIFIED, true));
                }
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_FLOAT,
                        AudioSystem.NOT_SPECIFIED, 32, channels, channels * 4,
                        AudioSystem.NOT_SPECIFIED, false));
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_FLOAT,
                        AudioSystem.NOT_SPECIFIED, 32, channels, channels * 4,
                        AudioSystem.NOT_SPECIFIED, true));
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_FLOAT,
                        AudioSystem.NOT_SPECIFIED, 64, channels, channels * 8,
                        AudioSystem.NOT_SPECIFIED, false));
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_FLOAT,
                        AudioSystem.NOT_SPECIFIED, 64, channels, channels * 8,
                        AudioSystem.NOT_SPECIFIED, true));
            }
            AudioFormat[] formats_array = formats.ToArray();
            sourceLineInfo[0] = new DataLine.Info(typeof(ISourceDataLine),
                    formats_array, AudioSystem.NOT_SPECIFIED,
                    AudioSystem.NOT_SPECIFIED);
            sourceLineInfo[1] = new DataLine.Info(typeof(IClip), formats_array,
                    AudioSystem.NOT_SPECIFIED, AudioSystem.NOT_SPECIFIED);
        }

        public ILine getLine(Line.Info info) {

            if (!isLineSupported(info))
                throw new ArgumentException("Line unsupported: " + info);

            if ((info.getLineClass() == typeof(ISourceDataLine))) {
                return new SoftMixingSourceDataLine(this, (DataLine.Info)info);
            }
            if ((info.getLineClass() == typeof(IClip))) {
                return new SoftMixingClip(this, (DataLine.Info)info);
            }

            throw new ArgumentException("Line unsupported: " + info);
        }

        public int getMaxLines(Line.Info info) {
            if (info.getLineClass() == typeof(ISourceDataLine))
                return AudioSystem.NOT_SPECIFIED;
            if (info.getLineClass() == typeof(IClip))
                return AudioSystem.NOT_SPECIFIED;
            return 0;
        }

        public Mixer.Info getMixerInfo() {
            return info;
        }

        public Line.Info[] getSourceLineInfo() {
            Line.Info[] localArray = new Line.Info[sourceLineInfo.Length];
            Array.Copy(sourceLineInfo, 0, localArray, 0,
                    sourceLineInfo.Length);
            return localArray;
        }

        public Line.Info[] getSourceLineInfo(
                Line.Info info) {
            int i;
            List<Line.Info> infos = new List<Line.Info>();

            for (i = 0; i < sourceLineInfo.Length; i++) {
                if (info.matches(sourceLineInfo[i])) {
                    infos.Add(sourceLineInfo[i]);
                }
            }
            return infos.ToArray();
        }

        public ILine[] getSourceLines() {

            ILine[] localLines;

            lock (control_mutex) {

                if (mainmixer == null)
                    return new ILine[0];
                SoftMixingDataLine[] sourceLines = mainmixer.getOpenLines();

                localLines = new ILine[sourceLines.Length];

                for (int i = 0; i < localLines.Length; i++) {
                    localLines[i] = sourceLines[i];
                }
            }

            return localLines;
        }

        public Line.Info[] getTargetLineInfo() {
            return new Line.Info[0];
        }

        public Line.Info[] getTargetLineInfo(
                Line.Info info) {
            return new Line.Info[0];
        }

        public ILine[] getTargetLines() {
            return new ILine[0];
        }

        public bool isLineSupported(Line.Info info) {
            if (info != null) {
                for (int i = 0; i < sourceLineInfo.Length; i++) {
                    if (info.matches(sourceLineInfo[i])) {
                        return true;
                    }
                }
            }
            return false;
        }

        public bool isSynchronizationSupported(ILine[] lines, bool maintainSync) {
            return false;
        }

        public void synchronize(ILine[] lines, bool maintainSync) {
            throw new ArgumentException(
                    "Synchronization not supported by this mixer.");
        }

        public void unsynchronize(ILine[] lines) {
            throw new ArgumentException(
                    "Synchronization not supported by this mixer.");
        }

        public void addLineListener(ILineListener listener) {
            lock (control_mutex) {
                listeners.Add(listener);
            }
        }

        private void sendEvent(LineEvent evnt) {
            if (listeners.Count == 0)
                return;
            ILineListener[] listener_array = listeners.ToArray();
            foreach (ILineListener listener in listener_array) {
                listener.update(evnt);
            }
        }

        public void close() {
            if (!isOpen())
                return;

            sendEvent(new LineEvent(this, LineEvent.Type.CLOSE,
                    AudioSystem.NOT_SPECIFIED));

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
                // Pusher must not be closed synchronized against control_mutex
                // this may result in synchronized conflict between pusher and
                // current thread.
                pusher_to_be_closed.stop();

                try {
                    pusher_stream_to_be_closed.Close();
                }
                catch (IOException e) {
                    Printer.printStackTrace(e);
                }
            }

            lock (control_mutex) {

                if (mainmixer != null)
                    mainmixer.close();
                _open = false;

                if (sourceDataLine != null) {
                    sourceDataLine.drain();
                    sourceDataLine.close();
                    sourceDataLine = null;
                }
            }
        }

        public void Dispose() {
            close();
        }

        public Control getControl(Control.Type control) {
            throw new ArgumentException("Unsupported control type : "
                    + control);
        }

        public Control[] getControls() {
            return new Control[0];
        }

        public Line.Info getLineInfo() {
            return new Line.Info(typeof(IMixer));
        }

        public bool isControlSupported(Control.Type control) {
            return false;
        }

        public bool isOpen() {
            lock (control_mutex) {
                return _open;
            }
        }

        public void open() {
            if (isOpen()) {
                implicitOpen = false;
                return;
            }
            open(null);
        }

        public void open(ISourceDataLine line) {
            if (isOpen()) {
                implicitOpen = false;
                return;
            }
            lock (control_mutex) {

                try {

                    if (line != null)
                        format = line.getFormat();

                    AudioInputStream ais = openStream(getFormat());

                    if (line == null) {
                        lock (SoftMixingMixerProvider.mutex) {
                            SoftMixingMixerProvider.lockthread = Thread.CurrentThread;
                        }

                        try {
                            IMixer defaultmixer = AudioSystem.getMixer(null);
                            if (defaultmixer != null) {
                                // Search for suitable line

                                DataLine.Info idealinfo = null;
                                AudioFormat idealformat = null;

                                Line.Info[] lineinfos = defaultmixer.getSourceLineInfo();
                                //idealFound:
                                for (int i = 0; i < lineinfos.Length; i++) {
                                    if (lineinfos[i].getLineClass() == typeof(ISourceDataLine)) {
                                        DataLine.Info info0 = (DataLine.Info)lineinfos[i];
                                        AudioFormat[] formats = info0.getFormats();
                                        for (int j = 0; j < formats.Length; j++) {
                                            AudioFormat format0 = formats[j];
                                            if (format0.getChannels() == 2 ||
                                                    format0.getChannels() == AudioSystem.NOT_SPECIFIED)
                                                if (format0.getEncoding().Equals(AudioFormat.Encoding.PCM_SIGNED) ||
                                                        format0.getEncoding().Equals(AudioFormat.Encoding.PCM_UNSIGNED))
                                                    if (format0.getSampleRate() == AudioSystem.NOT_SPECIFIED ||
                                                            format0.getSampleRate() == 48000.0)
                                                        if (format0.getSampleSizeInBits() == AudioSystem.NOT_SPECIFIED ||
                                                                format0.getSampleSizeInBits() == 16) {
                                                            idealinfo = info0;
                                                            int ideal_channels = format0.getChannels();
                                                            bool ideal_signed = format0.getEncoding().Equals(AudioFormat.Encoding.PCM_SIGNED);
                                                            float ideal_rate = format0.getSampleRate();
                                                            bool ideal_endian = format0.isBigEndian();
                                                            int ideal_bits = format0.getSampleSizeInBits();
                                                            if (ideal_bits == AudioSystem.NOT_SPECIFIED) ideal_bits = 16;
                                                            if (ideal_channels == AudioSystem.NOT_SPECIFIED) ideal_channels = 2;
                                                            if (ideal_rate == AudioSystem.NOT_SPECIFIED) ideal_rate = 48000;
                                                            idealformat = new AudioFormat(ideal_rate, ideal_bits,
                                                                    ideal_channels, ideal_signed, ideal_endian);
                                                            break;     //a@ break idealFound;
                                                        }
                                        }
                                        if (idealformat != null) break; //a@ break idealFound;
                                    }
                                }

                                if (idealformat != null) {
                                    format = idealformat;
                                    line = (ISourceDataLine)defaultmixer.getLine(idealinfo);
                                }
                            }

                            if (line == null)
                                line = AudioSystem.getSourceDataLine(format);
                        }
                        finally {
                            lock (SoftMixingMixerProvider.mutex) {
                                SoftMixingMixerProvider.lockthread = null;
                            }
                        }

                        if (line == null)
                            throw new ArgumentException("No line matching "
                                    + info.ToString() + " is supported.");
                    }

                    double latency = this.latency;

                    if (!line.isOpen()) {
                        int bufferSize = getFormat().getFrameSize()
                                * (int)(getFormat().getFrameRate() * (latency / 1000000f));
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
                    }
                    catch (IOException) {
                    }

                    // Tell mixer not fill read buffers fully.
                    // This lowers latency, and tells DataPusher
                    // to read in smaller amounts.
                    // mainmixer.readfully = false;
                    // pusher = new DataPusher(line, ais);

                    int buffersize = line.getBufferSize();
                    buffersize -= buffersize % controlbuffersize;

                    if (buffersize < 3 * controlbuffersize)
                        buffersize = 3 * controlbuffersize;

                    if (jitter_correction) {
                        ais = new SoftJitterCorrector(ais, buffersize,
                                controlbuffersize);
                    }
                    pusher = new SoftAudioPusher(line, ais, controlbuffersize);
                    pusher_stream = ais;
                    pusher.start();

                }
                catch (LineUnavailableException e) {
                    if (isOpen())
                        close();
                    throw new LineUnavailableException(e.ToString());
                }

            }
        }

        public AudioInputStream openStream(AudioFormat targetFormat) {

            if (isOpen())
                throw new LineUnavailableException("Mixer is already open");

            lock (control_mutex) {

                _open = true;

                implicitOpen = false;

                if (targetFormat != null)
                    format = targetFormat;

                mainmixer = new SoftMixingMainMixer(this);

                sendEvent(new LineEvent(this, LineEvent.Type.OPEN,
                        AudioSystem.NOT_SPECIFIED));

                return mainmixer.getInputStream();

            }

        }

        public void removeLineListener(ILineListener listener) {
            lock (control_mutex) {
                listeners.Remove(listener);
            }
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

        internal float getControlRate() {
            return controlrate;
        }

        internal SoftMixingMainMixer getMainMixer() {
            if (!isOpen())
                return null;
            return mainmixer;
        }
    }
}
