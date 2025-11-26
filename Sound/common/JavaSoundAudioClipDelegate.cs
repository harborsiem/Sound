/*
 * Copyright (c) 1999, 2025, Oracle and/or its affiliates. All rights reserved.
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
//import java.io.ByteArrayOutputStream;
//import java.io.File;
//import java.io.FileInputStream;
//import java.io.IOException;
//import java.io.InputStream;

//import javax.sound.SoundClip;
//import javax.sound.midi.InvalidMidiDataException;
//import javax.sound.midi.MetaEventListener;
//import javax.sound.midi.MetaMessage;
//import javax.sound.midi.MidiFileFormat;
//import javax.sound.midi.MidiSystem;
//import javax.sound.midi.MidiUnavailableException;
//import javax.sound.midi.Sequence;
//import javax.sound.midi.Sequencer;
//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.Clip;
//import javax.sound.sampled.DataLine;
//import javax.sound.sampled.LineEvent;
//import javax.sound.sampled.LineListener;
//import javax.sound.sampled.SourceDataLine;
//import javax.sound.sampled.UnsupportedAudioFileException;
//import com.sun.media.sound.JavaSoundAudioClip.AudioClipDisposerRecord;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using SystemX.Addon;
using SystemX.Media.Sound;
using SystemX.Sound.Midi;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

    /**
     * Java Sound audio clip;
     *
     */
    public sealed class JavaSoundAudioClipDelegate : IMetaEventListener, ILineListener {

        private long lastPlayCall = 0;
        private const int MINIMUM_PLAY_DELAY = 30;

        private byte[] loadedAudio = null;
        private int loadedAudioByteLength = 0;
        private AudioFormat loadedAudioFormat = null;

        private IAutoClosingClip clip = null;
        private bool clipLooping = false;
        private bool clipPlaying = false;

        private DataPusher datapusher = null;

        private ISequencer sequencer = null;
        private Sequence sequence = null;
        private bool sequencerloop = false;
        private volatile bool success;

        /**
         * used for determining how many samples is the
         * threshold between playing as a Clip and streaming
         * from the file.
         *
         * $$jb: 11.07.99: the engine has a limit of 1M
         * samples to play as a Clip, so compare this number
         * with the number of samples in the stream.
         *
         */
        private const long CLIP_THRESHOLD = 1048576;
        private const int STREAM_BUFFER_SIZE = 1024;

        private AudioClipDisposerRecord disposerRecord;
        JavaSoundAudioClipDelegate(FileInfo file, AudioClipDisposerRecord record) {
            this.disposerRecord = record;
            using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read)) {
                init(stream);
            }

        }

        private void init(Stream input) {
            BufferedStream bis = new BufferedStream(input, STREAM_BUFFER_SIZE);
            long markPosition = bis.Position; //bis.mark(STREAM_BUFFER_SIZE);
            try {
                AudioInputStream ais = AudioSystem.getAudioInputStream(bis);
                // load the stream data into memory
                success = loadAudioData(ais);

                if (success) {
                    success = false;
                    if (loadedAudioByteLength < CLIP_THRESHOLD) {
                        success = createClip();
                    }
                    if (!success) {
                        success = createSourceDataLine();
                    }
                }
            } catch (UnsupportedAudioFileException e) {
                try {
                    MidiFileFormat mff = MidiSystem.getMidiFileFormat(bis);
                    success = createSequencer(bis);
                } catch (InvalidMidiDataException e1) {
                    success = false;
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool canPlay() {
            return success;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool isPlaying() {
            if (!canPlay()) {
                return false;
            } else if (clip != null) {
                return clipPlaying;
            } else if (datapusher != null) {
                return datapusher.isPlaying();
            } else if (sequencer != null) {
                return sequencer.isRunning();
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void play() {
            if (!success) {
                return;
            }
            startImpl(false);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void loop() {
            if (!success) {
                return;
            }
            startImpl(true);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void startImpl(bool loop) {
            // hack for some applications that call the start method very rapidly...
            long currentTime = Environment.TickCount;
            long diff = currentTime - lastPlayCall;
            if (diff < MINIMUM_PLAY_DELAY) {
                return;
            }
            lastPlayCall = currentTime;
            try {
                if (clip != null) {
                    // We need to disable autoclosing mechanism otherwise the clip
                    // can be closed after "!clip.isOpen()" check, because of
                    // previous inactivity.
                    clip.setAutoClosing(false);
                    try {
                        if (!clip.isOpen()) {
                            clip.open(loadedAudioFormat, loadedAudio, 0,
                                      loadedAudioByteLength);
                        } else {
                            clip.flush();
                            if (loop != clipLooping) {
                                // need to stop in case the looped status changed
                                clip.stop();
                            }
                        }
                        clip.setFramePosition(0);
                        if (loop) {
                            clip.loop(Clip.LOOP_CONTINUOUSLY);
                        } else {
                            clip.start();
                        }
                        clipLooping = loop;
                    } finally {
                        clip.setAutoClosing(true);
                    }
                } else if (datapusher != null) {
                    datapusher.start(loop);

                } else if (sequencer != null) {
                    sequencerloop = loop;
                    if (sequencer.isRunning()) {
                        sequencer.setMicrosecondPosition(0);
                    }
                    if (!sequencer.isOpen()) {
                        try {
                            sequencer.open();
                            sequencer.setSequence(sequence);

                        } catch (Exception ex) {
                            if (ex is InvalidMidiDataException || ex is MidiUnavailableException) {
                                if (Printer.err) printStackTrace(ex);
                            } else
                                throw;
                        }
                    }
                    sequencer.addMetaEventListener(this);
                    try {
                        sequencer.start();
                    } catch (Exception e) {
                        if (Printer.err) printStackTrace(e);
                    }
                }
            } catch (Exception e) {
                if (Printer.err) printStackTrace(e);
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void stop() {
            if (!success) {
                return;
            }
            lastPlayCall = 0;

            if (clip != null) {
                try {
                    clip.flush();
                } catch (Exception e1) {
                    if (Printer.err) printStackTrace(e1);
                }
                try {
                    clip.stop();
                } catch (Exception e2) {
                    if (Printer.err) printStackTrace(e2);
                }
            } else if (datapusher != null) {
                datapusher.stop();
            } else if (sequencer != null) {
                try {
                    sequencerloop = false;
                    sequencer.removeMetaEventListener(this);
                    sequencer.stop();
                } catch (Exception e3) {
                    if (Printer.err) printStackTrace(e3);
                }
                try {
                    sequencer.close();
                } catch (Exception e4) {
                    if (Printer.err) printStackTrace(e4);
                }
            }
        }

        // Event handlers (for debugging)

        //@Override
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void update(LineEvent evnt) {
            if (clip != null) {
                if (clip == evnt.getSource()) {
                    if (evnt.getType() == LineEvent.Type.START) {
                        clipPlaying = true;
                    } else if ((evnt.getType() == LineEvent.Type.STOP) ||
                               (evnt.getType() == LineEvent.Type.CLOSE)) {
                        clipPlaying = false;
                    }
                }
            }
        }

        // handle MIDI track end meta events for looping

        //@Override
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void meta(MetaMessage message) {
            if (message.getType() == 47) {
                if (sequencerloop) {
                    //notifyAll();
                    sequencer.setMicrosecondPosition(0);
                    loop();
                } else {
                    stop();
                }
            }
        }

        //@Override
        public override String ToString() {
            return GetType().ToString();
        }

        // FILE LOADING METHODS

        private bool loadAudioData(AudioInputStream ais) {
            // first possibly convert this stream to PCM
            ais = Toolkit.getPCMConvertedAudioInputStream(ais);
            if (ais == null) {
                return false;
            }

            loadedAudioFormat = ais.getFormat();
            long frameLen = ais.getFrameLength();
            int frameSize = loadedAudioFormat.getFrameSize();
            long byteLen = AudioSystem.NOT_SPECIFIED;
            if (frameLen != AudioSystem.NOT_SPECIFIED
                && frameLen > 0
                && frameSize != AudioSystem.NOT_SPECIFIED
                && frameSize > 0) {
                byteLen = frameLen * frameSize;
            }
            if (byteLen != AudioSystem.NOT_SPECIFIED) {
                // if the stream length is known, it can be efficiently loaded into memory
                readStream(ais, byteLen);
            } else {
                // otherwise we use a ByteArrayOutputStream to load it into memory
                readStream(ais);
            }

            // if everything went fine, we have now the audio data in
            // loadedAudio, and the byte length in loadedAudioByteLength
            return true;
        }

        private void readStream(AudioInputStream ais, long byteLen) {
            // arrays "only" max. 2GB
            int intLen;
            if (byteLen > 2147483647) {
                intLen = 2147483647;
            } else {
                intLen = (int)byteLen;
            }
            loadedAudio = new byte[intLen];
            loadedAudioByteLength = 0;

            // this loop may throw an IOException
            while (true) {
                int bytesRead = ais.Read(loadedAudio, loadedAudioByteLength, intLen - loadedAudioByteLength);
                if (bytesRead <= 0) {
                    ais.Close();
                    break;
                }
                loadedAudioByteLength += bytesRead;
            }
        }

        private void readStream(AudioInputStream ais) {

            DirectBAOS baos = new DirectBAOS();
            int totalBytesRead;
            using (ais) {
                totalBytesRead = (int)ais.transferTo(baos);
            }
            loadedAudio = baos.getInternalBuffer();
            loadedAudioByteLength = totalBytesRead;
        }

        // METHODS FOR CREATING THE DEVICE

        private bool createClip() {
            try {
                DataLine.Info info = new DataLine.Info(typeof(IClip), loadedAudioFormat);
                if (!(AudioSystem.isLineSupported(info))) {
                    if (Printer.err) Printer.Err("Clip not supported: " + loadedAudioFormat);
                    // fail silently
                    return false;
                }
                Object line = AudioSystem.getLine(info);
                if (!(line is IAutoClosingClip)) {
                    if (Printer.err) Printer.Err("Clip is not auto closing!" + clip);
                    // fail -> will try with SourceDataLine
                    return false;
                }
                clip = (IAutoClosingClip)line;
                disposerRecord.setClip(clip);
                clip.setAutoClosing(true);
                clip.addLineListener(this);
            } catch (Exception e) {
                if (Printer.err) printStackTrace(e);
                // fail silently
                return false;
            }

            if (clip == null) {
                // fail silently
                return false;
            }
            return true;
        }

        private bool createSourceDataLine() {
            try {
                DataLine.Info info = new DataLine.Info(typeof(ISourceDataLine), loadedAudioFormat);
                if (!(AudioSystem.isLineSupported(info))) {
                    if (Printer.err) Printer.Err("Line not supported: " + loadedAudioFormat);
                    // fail silently
                    return false;
                }
                ISourceDataLine source = (ISourceDataLine)AudioSystem.getLine(info);
                datapusher = new DataPusher(source, loadedAudioFormat, loadedAudio, loadedAudioByteLength, true);
                disposerRecord.setDataPusher(datapusher);
            } catch (Exception e) {
                if (Printer.err) printStackTrace(e);
                // fail silently
                return false;
            }

            if (datapusher == null) {
                // fail silently
                return false;
            }
            return true;
        }

        private bool createSequencer(BufferedStream input) {
            // get the sequencer
            try {
                sequencer = MidiSystem.getSequencer();
                disposerRecord.setSequencer(sequencer);
            } catch (MidiUnavailableException me) {
                if (Printer.err) printStackTrace(me);
                return false;
            }
            if (sequencer == null) {
                return false;
            }

            try {
                sequence = MidiSystem.getSequence(input);
                if (sequence == null) {
                    return false;
                }
            } catch (InvalidMidiDataException e) {
                if (Printer.err) printStackTrace(e);
                return false;
            }
            return true;
        }

        /*
         * private inner class representing a ByteArrayOutputStream
         * which allows retrieval of the internal array
         */
        private class DirectBAOS : MemoryStream {
            internal DirectBAOS()
                : base() {
            }

            public byte[] getInternalBuffer() {
                return GetBuffer(); // buf;
            }

        } // class DirectBAOS

        private void printStackTrace(Exception ex) {
            Printer.printStackTrace(ex);
        }
    }
}
