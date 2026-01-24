/*
 * Copyright (c) 2003, 2019, Oracle and/or its affiliates. All rights reserved.
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
//import java.util.ArrayList;
//import java.util.List;
//import java.util.Map;
//import java.util.WeakHashMap;

//import javax.sound.midi.ControllerEventListener;
//import javax.sound.midi.InvalidMidiDataException;
//import javax.sound.midi.MetaEventListener;
//import javax.sound.midi.MetaMessage;
//import javax.sound.midi.MidiDevice;
//import javax.sound.midi.MidiEvent;
//import javax.sound.midi.MidiMessage;
//import javax.sound.midi.MidiSystem;
//import javax.sound.midi.MidiUnavailableException;
//import javax.sound.midi.Receiver;
//import javax.sound.midi.Sequence;
//import javax.sound.midi.Sequencer;
//import javax.sound.midi.ShortMessage;
//import javax.sound.midi.Synthesizer;
//import javax.sound.midi.Track;
//import javax.sound.midi.Transmitter;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Runtime.CompilerServices;
using SystemX.Addon;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {

/**
 * A Real Time Sequencer
 *
 * @author Florian Bomers
 */

/* TODO:
 * - rename PlayThread to PlayEngine (because isn't a thread)
 */
    internal sealed class RealTimeSequencer : AbstractMidiDevice, ISequencer, IAutoConnectSequencer {

        /**
         * Event Dispatcher thread. Should be using a shared event
         * dispatcher instance with a factory in EventDispatcher
         */
        //ConditionalWeakTable (.NET4)
        //<Thread, EventDispatcher>
        private static readonly Dictionary<Thread, WeakReference> dispatchers =
                new Dictionary<Thread, WeakReference>();

        /**
         * All RealTimeSequencers share this info object.
         */
        internal static readonly MidiDevice.Info info = new RealTimeSequencerInfo();


        private static readonly Sequencer.SyncMode[] masterSyncModes = { Sequencer.SyncMode.INTERNAL_CLOCK };
        private static readonly Sequencer.SyncMode[] slaveSyncModes = { Sequencer.SyncMode.NO_SYNC };

        private static readonly Sequencer.SyncMode masterSyncMode = Sequencer.SyncMode.INTERNAL_CLOCK;
        private static readonly Sequencer.SyncMode slaveSyncMode = Sequencer.SyncMode.NO_SYNC;


        /**
         * Sequence on which this sequencer is operating.
         */
        private Sequence sequence = null;

        // caches

        /**
         * Same for setTempoInMPQ...
         * -1 means not set.
         */
        private double cacheTempoMPQ = -1;

        /**
         * cache value for tempo factor until sequence is set
         * -1 means not set.
         */
        private float cacheTempoFactor = -1;

        /** if a particular track is muted */
        private bool[] trackMuted = null;
        /** if a particular track is solo */
        private bool[] trackSolo = null;

        /** tempo cache for getMicrosecondPosition */
        private readonly MidiUtils.TempoCache tempoCache = new MidiUtils.TempoCache();

        /**
         * True if the sequence is running.
         */
        private volatile bool running;

        /**
         * the thread for pushing out the MIDI messages.
         */
        private PlayThread playThread;

        /**
         * True if we are recording.
         */
        private volatile bool recording;

        /**
         * List of tracks to which we're recording.
         */
        private readonly List<RecordingTrack> recordingTracks = new List<RecordingTrack>();

        private long loopStart = 0;
        private long loopEnd = -1;
        private int loopCount = 0;

        /**
         * Meta event listeners.
         */
        private readonly List<Object> metaEventListeners = new List<Object>();

        /**
         * Control change listeners.
         */
        private readonly List<ControllerListElement> controllerEventListeners = new List<ControllerListElement>();

        /**
         * automatic connection support.
         */
        private bool autoConnect = false;

        /**
         * if we need to autoconnect at next open.
         */
        private bool doAutoConnectAtNextOpen = false;

        /**
         * the receiver that this device is auto-connected to.
         */
        IReceiver autoConnectedReceiver = null;


        /* ****************************** CONSTRUCTOR ****************************** */

        internal RealTimeSequencer()
            : base(info) {
        }

        /* ****************************** SEQUENCER METHODS ******************** */

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void setSequence(Sequence sequence) {
            if (sequence != this.sequence) {
                if (this.sequence != null && sequence == null) {
                    setCaches();
                    stop();
                    // initialize some non-cached values
                    trackMuted = null;
                    trackSolo = null;
                    loopStart = 0;
                    loopEnd = -1;
                    loopCount = 0;
                    if (getDataPump() != null) {
                        getDataPump().setTickPos(0);
                        getDataPump().resetLoopCount();
                    }
                }

                if (playThread != null) {
                    playThread.setSequence(sequence);
                }

                // store this sequence (do not copy - we want to give the possibility
                // of modifying the sequence at runtime)
                this.sequence = sequence;

                if (sequence != null) {
                    tempoCache.refresh(sequence);
                    // rewind to the beginning
                    setTickPosition(0);
                    // propagate caches
                    propagateCaches();
                }
            } else if (sequence != null) {
                tempoCache.refresh(sequence);
                if (playThread != null) {
                    playThread.setSequence(sequence);
                }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void setSequence(Stream stream) {
            if (stream == null) {
                setSequence((Sequence)null);
                return;
            }

            Sequence seq = MidiSystem.getSequence(stream); // can throw IOException, InvalidMidiDataException

            setSequence(seq);
        }

        public Sequence getSequence() {
            return sequence;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void start() {
            // sequencer not open: throw an exception
            if (!isOpen()) {
                throw new InvalidOperationException("sequencer not open");
            }

            // sequence not available: throw an exception
            if (sequence == null) {
                throw new InvalidOperationException("sequence not set");
            }

            // already running: return quietly
            if (running == true) {
                return;
            }

            // start playback
            implStart();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void stop() {
            if (!isOpen()) {
                throw new InvalidOperationException("sequencer not open");
            }
            stopRecording();

            // not running; just return
            if (running == false) {
                return;
            }

            // stop playback
            implStop();
        }

        public bool isRunning() {
            return running;
        }

        public void startRecording() {
            if (!isOpen()) {
                throw new InvalidOperationException("Sequencer not open");
            }

            start();
            recording = true;
        }

        public void stopRecording() {
            if (!isOpen()) {
                throw new InvalidOperationException("Sequencer not open");
            }
            recording = false;
        }

        public bool isRecording() {
            return recording;
        }

        public void recordEnable(Track track, int channel) {
            if (!findTrack(track)) {
                throw new ArgumentException("Track does not exist in the current sequence");
            }

            lock (recordingTracks) {
                RecordingTrack rc = RecordingTrack.get(recordingTracks, track);
                if (rc != null) {
                    rc.channel = channel;
                } else {
                    recordingTracks.Add(new RecordingTrack(track, channel));
                }
            }

        }

        public void recordDisable(Track track) {
            lock (recordingTracks) {
                RecordingTrack rc = RecordingTrack.get(recordingTracks, track);
                if (rc != null) {
                    recordingTracks.Remove(rc);
                }
            }

        }

        private bool findTrack(Track track) {
            bool found = false;
            if (sequence != null) {
                Track[] tracks = sequence.getTracks();
                for (int i = 0; i < tracks.Length; i++) {
                    if (track == tracks[i]) {
                        found = true;
                        break;
                    }
                }
            }
            return found;
        }

        public float getTempoInBPM() {
            return (float)MidiUtils.convertTempo(getTempoInMPQ());
        }

        public void setTempoInBPM(float bpm) {
            if (bpm <= 0) {
                // should throw IllegalArgumentException
                bpm = 1.0f;
            }

            setTempoInMPQ((float)MidiUtils.convertTempo((double)bpm));
        }

        public float getTempoInMPQ() {
            if (needCaching()) {
                // if the sequencer is closed, return cached value
                if (cacheTempoMPQ != -1) {
                    return (float)cacheTempoMPQ;
                }
                // if sequence is set, return current tempo
                if (sequence != null) {
                    return tempoCache.getTempoMPQAt(getTickPosition());
                }

                // last resort: return a standard tempo: 120bpm
                return (float)MidiUtils.DEFAULT_TEMPO_MPQ;
            }
            return getDataPump().getTempoMPQ();
        }

        public void setTempoInMPQ(float mpq) {
            if (mpq <= 0) {
                // should throw IllegalArgumentException
                mpq = 1.0f;
            }
            if (needCaching()) {
                // cache the value
                cacheTempoMPQ = mpq;
            } else {
                // set the native tempo in MPQ
                getDataPump().setTempoMPQ(mpq);

                // reset the tempoInBPM and tempoInMPQ values so we won't use them again
                cacheTempoMPQ = -1;
            }
        }

        public void setTempoFactor(float factor) {
            if (factor <= 0) {
                // should throw IllegalArgumentException
                return;
            }
            if (needCaching()) {
                cacheTempoFactor = factor;
            } else {
                getDataPump().setTempoFactor(factor);
                // don't need cache anymore
                cacheTempoFactor = -1;
            }
        }

        public float getTempoFactor() {
            if (needCaching()) {
                if (cacheTempoFactor != -1) {
                    return cacheTempoFactor;
                }
                return 1.0f;
            }
            return getDataPump().getTempoFactor();
        }

        public long getTickLength() {
            if (sequence == null) {
                return 0;
            }

            return sequence.getTickLength();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public long getTickPosition() {
            if (getDataPump() == null || sequence == null) {
                return 0;
            }

            return getDataPump().getTickPos();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void setTickPosition(long tick) {
            if (tick < 0) {
                // should throw IllegalArgumentException
                return;
            }

            if (getDataPump() == null) {
                if (tick != 0) {
                    // throw new InvalidStateException("cannot set position in closed state");
                }
            } else if (sequence == null) {
                if (tick != 0) {
                    // throw new InvalidStateException("cannot set position if sequence is not set");
                }
            } else {
                getDataPump().setTickPos(tick);
            }
        }

        public long getMicrosecondLength() {
            if (sequence == null) {
                return 0;
            }

            return sequence.getMicrosecondLength();
        }

        public override long getMicrosecondPosition() {
            if (getDataPump() == null || sequence == null) {
                return 0;
            }
            lock (tempoCache) {
                return MidiUtils.tick2microsecond(sequence, getDataPump().getTickPos(), tempoCache);
            }
        }

        public void setMicrosecondPosition(long microseconds) {
            if (microseconds < 0) {
                // should throw IllegalArgumentException
                return;
            }
            if (getDataPump() == null) {
                if (microseconds != 0) {
                    // throw new InvalidStateException("cannot set position in closed state");
                }
            } else if (sequence == null) {
                if (microseconds != 0) {
                    // throw new InvalidStateException("cannot set position if sequence is not set");
                }
            } else {
                lock (tempoCache) {
                    setTickPosition(MidiUtils.microsecond2tick(sequence, microseconds, tempoCache));
                }
            }
        }

        public void setMasterSyncMode(Sequencer.SyncMode sync) {
            // not supported
        }

        public Sequencer.SyncMode getMasterSyncMode() {
            return masterSyncMode;
        }

        public Sequencer.SyncMode[] getMasterSyncModes() {
            Sequencer.SyncMode[] returnedModes = new Sequencer.SyncMode[masterSyncModes.Length];
            Array.Copy(masterSyncModes, 0, returnedModes, 0, masterSyncModes.Length);
            return returnedModes;
        }

        public void setSlaveSyncMode(Sequencer.SyncMode sync) {
            // not supported
        }

        public Sequencer.SyncMode getSlaveSyncMode() {
            return slaveSyncMode;
        }

        public Sequencer.SyncMode[] getSlaveSyncModes() {
            Sequencer.SyncMode[] returnedModes = new Sequencer.SyncMode[slaveSyncModes.Length];
            Array.Copy(slaveSyncModes, 0, returnedModes, 0, slaveSyncModes.Length);
            return returnedModes;
        }

        internal int getTrackCount() {
            Sequence seq = getSequence();
            if (seq != null) {
                // $$fb wish there was a nicer way to get the number of tracks...
                return sequence.getTracks().Length;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void setTrackMute(int track, bool mute) {
            int trackCount = getTrackCount();
            if (track < 0 || track >= getTrackCount()) return;
            trackMuted = ensureBoolArraySize(trackMuted, trackCount);
            trackMuted[track] = mute;
            if (getDataPump() != null) {
                getDataPump().muteSoloChanged();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool getTrackMute(int track) {
            if (track < 0 || track >= getTrackCount()) return false;
            if (trackMuted == null || trackMuted.Length <= track) return false;
            return trackMuted[track];
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void setTrackSolo(int track, bool solo) {
            int trackCount = getTrackCount();
            if (track < 0 || track >= getTrackCount()) return;
            trackSolo = ensureBoolArraySize(trackSolo, trackCount);
            trackSolo[track] = solo;
            if (getDataPump() != null) {
                getDataPump().muteSoloChanged();
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool getTrackSolo(int track) {
            if (track < 0 || track >= getTrackCount()) return false;
            if (trackSolo == null || trackSolo.Length <= track) return false;
            return trackSolo[track];
        }

        public bool addMetaEventListener(IMetaEventListener listener) {
            lock (metaEventListeners) {
                if (!metaEventListeners.Contains(listener)) {

                    metaEventListeners.Add(listener);
                }
                return true;
            }
        }

        public void removeMetaEventListener(IMetaEventListener listener) {
            lock (metaEventListeners) {
                int index = metaEventListeners.IndexOf(listener);
                if (index >= 0) {
                    metaEventListeners.RemoveAt(index);
                }
            }
        }

        public int[] addControllerEventListener(IControllerEventListener listener, int[] controllers) {
            lock (controllerEventListeners) {

                // first find the listener.  if we have one, add the controllers
                // if not, create a new element for it.
                ControllerListElement cve = null;
                bool flag = false;
                for (int i = 0; i < controllerEventListeners.Count; i++) {

                    cve = controllerEventListeners[i];

                    if (cve.listener.Equals(listener)) {
                        cve.addControllers(controllers);
                        flag = true;
                        break;
                    }
                }
                if (!flag) {
                    cve = new ControllerListElement(listener, controllers);
                    controllerEventListeners.Add(cve);
                }

                // and return all the controllers this listener is interested in
                return cve.getControllers();
            }
        }

        public int[] removeControllerEventListener(IControllerEventListener listener, int[] controllers) {
            lock (controllerEventListeners) {
                ControllerListElement cve = null;
                bool flag = false;
                for (int i = 0; i < controllerEventListeners.Count; i++) {
                    cve = controllerEventListeners[i];
                    if (cve.listener.Equals(listener)) {
                        cve.removeControllers(controllers);
                        flag = true;
                        break;
                    }
                }
                if (!flag) {
                    return new int[0];
                }
                if (controllers == null) {
                    int index = controllerEventListeners.IndexOf(cve);
                    if (index >= 0) {
                        controllerEventListeners.RemoveAt(index);
                    }
                    return new int[0];
                }
                return cve.getControllers();
            }
        }

        ////////////////// LOOPING (added in 1.5) ///////////////////////

        public void setLoopStartPoint(long tick) {
            if ((tick > getTickLength())
                || ((loopEnd != -1) && (tick > loopEnd))
                || (tick < 0)) {
                throw new ArgumentException("invalid loop start point: " + tick);
            }
            loopStart = tick;
        }

        public long getLoopStartPoint() {
            return loopStart;
        }

        public void setLoopEndPoint(long tick) {
            if ((tick > getTickLength())
                || ((loopStart > tick) && (tick != -1))
                || (tick < -1)) {
                throw new ArgumentException("invalid loop end point: " + tick);
            }
            loopEnd = tick;
        }

        public long getLoopEndPoint() {
            return loopEnd;
        }

        public void setLoopCount(int count) {
            if (count != Sequencer.LOOP_CONTINUOUSLY
                && count < 0) {
                throw new ArgumentException("illegal value for loop count: " + count);
            }
            loopCount = count;
            if (getDataPump() != null) {
                getDataPump().resetLoopCount();
            }
        }

        public int getLoopCount() {
            return loopCount;
        }

        /* *********************************** play control ************************* */

        protected override void implOpen() {
            //openInternalSynth();

            // create PlayThread
            playThread = new PlayThread(this);

            //id = nOpen();
            //if (id == 0) {
            //    throw new MidiUnavailableException("unable to open sequencer");
            //}
            if (sequence != null) {
                playThread.setSequence(sequence);
            }

            // propagate caches
            propagateCaches();

            if (doAutoConnectAtNextOpen) {
                doAutoConnect();
            }
        }

        private void doAutoConnect() {
            IReceiver rec = null;
            // first try to connect to the default synthesizer
            // IMPORTANT: this code needs to be synch'ed with
            //            MidiSystem.getSequencer(boolean), because the same
            //            algorithm needs to be used!
            try {
                ISynthesizer synth = MidiSystem.getSynthesizer();
                if (synth is IReferenceCountingDevice) {
                    rec = ((IReferenceCountingDevice)synth).getReceiverReferenceCounting();
                } else {
                    synth.open();
                    try {
                        rec = synth.getReceiver();
                    } finally {
                        // make sure that the synth is properly closed
                        if (rec == null) {
                            synth.close();
                        }
                    }
                }
            } catch (Exception) {
                // something went wrong with synth
            }
            if (rec == null) {
                // then try to connect to the default Receiver
                try {
                    rec = MidiSystem.getReceiver();
                } catch (Exception) {
                    // something went wrong. Nothing to do then!
                }
            }
            if (rec != null) {
                autoConnectedReceiver = rec;
                try {
                    getTransmitter().setReceiver(rec);
                } catch (Exception) { }
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void propagateCaches() {
            // only set caches if open and sequence is set
            if (sequence != null && isOpen()) {
                if (cacheTempoFactor != -1) {
                    setTempoFactor(cacheTempoFactor);
                }
                if (cacheTempoMPQ == -1) {
                    setTempoInMPQ((new MidiUtils.TempoCache(sequence)).getTempoMPQAt(getTickPosition()));
                } else {
                    setTempoInMPQ((float)cacheTempoMPQ);
                }
            }
        }

        /**
         * populate the caches with the current values.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void setCaches() {
            cacheTempoFactor = getTempoFactor();
            cacheTempoMPQ = getTempoInMPQ();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        protected override void implClose() {
            if (playThread == null) {
                if (Printer.err) Printer.Err("RealTimeSequencer.implClose() called, but playThread not instantiated!");
            } else {
                // Interrupt playback loop.
                playThread.close();
                playThread = null;
            }

            base.implClose();

            sequence = null;
            running = false;
            cacheTempoMPQ = -1;
            cacheTempoFactor = -1;
            trackMuted = null;
            trackSolo = null;
            loopStart = 0;
            loopEnd = -1;
            loopCount = 0;

            /** if this sequencer is set to autoconnect, need to
             * re-establish the connection at next open!
             */
            doAutoConnectAtNextOpen = autoConnect;

            if (autoConnectedReceiver != null) {
                try {
                    autoConnectedReceiver.close();
                } catch (Exception) { }
                autoConnectedReceiver = null;
            }
        }

        internal void implStart() {
            if (playThread == null) {
                if (Printer.err) Printer.Err("RealTimeSequencer.implStart() called, but playThread not instantiated!");
                return;
            }

            tempoCache.refresh(sequence);
            if (!running) {
                running = true;
                playThread.start();
            }
        }

        internal void implStop() {
            if (playThread == null) {
                if (Printer.err) Printer.Err("RealTimeSequencer.implStop() called, but playThread not instantiated!");
                return;
            }

            recording = false;
            if (running) {
                running = false;
                playThread.stop();
            }
        }

        private static EventDispatcher getEventDispatcher() {
            // create and start the global event thread
            //TODO  need a way to stop this thread when the engine is done
            Thread tg = Thread.CurrentThread;
            lock (dispatchers) {
                EventDispatcher eventDispatcher = null;
                if (dispatchers.ContainsKey(tg)) {
                    eventDispatcher = dispatchers[tg].Target as EventDispatcher;
                }
                if (eventDispatcher == null) {
                    eventDispatcher = new EventDispatcher();
                    dispatchers[tg] = new WeakReference(eventDispatcher);
                    eventDispatcher.start();
                }
                return eventDispatcher;
            }
        }

        /**
         * Send midi player events.
         * must not be synchronized on "this"
         */
        internal void sendMetaEvents(MidiMessage message) {
            if (metaEventListeners.Count == 0) return;

            getEventDispatcher().sendAudioEvents(message, metaEventListeners);
        }

        /**
         * Send midi player events.
         */
        internal void sendControllerEvents(MidiMessage message) {
            int size = controllerEventListeners.Count;
            if (size == 0) return;

            if (!(message is ShortMessage)) {
                return;
            }
            ShortMessage msg = (ShortMessage)message;
            int controller = msg.getData1();
            List<Object> sendToListeners = new List<Object>();
            for (int i = 0; i < size; i++) {
                ControllerListElement cve = controllerEventListeners[i];
                for (int j = 0; j < cve.controllers.Length; j++) {
                    if (cve.controllers[j] == controller) {
                        sendToListeners.Add(cve.listener);
                        break;
                    }
                }
            }
            getEventDispatcher().sendAudioEvents(message, sendToListeners);
        }

        private bool needCaching() {
            return !isOpen() || (sequence == null) || (playThread == null);
        }

        /**
         * return the data pump instance, owned by play thread
         * if playthread is null, return null.
         * This method is guaranteed to return non-null if
         * needCaching returns false
         */
        private DataPump getDataPump() {
            if (playThread != null) {
                return playThread.getDataPump();
            }
            return null;
        }

        private MidiUtils.TempoCache getTempoCache() {
            return tempoCache;
        }

        private static bool[] ensureBoolArraySize(bool[] array, int desiredSize) {
            if (array == null) {
                return new bool[desiredSize];
            }
            if (array.Length < desiredSize) {
                bool[] newArray = new bool[desiredSize];
                Array.Copy(array, 0, newArray, 0, array.Length);
                return newArray;
            }
            return array;
        }

        // OVERRIDES OF ABSTRACT MIDI DEVICE METHODS

        protected override bool hasReceivers() {
            return true;
        }

        // for recording
        protected override IReceiver createReceiver() {
            return new SequencerReceiver(this);
        }

        protected override bool hasTransmitters() {
            return true;
        }

        protected override ITransmitter createTransmitter() {
            return new SequencerTransmitter(this);
        }

        // interface AutoConnectSequencer
        public void setAutoConnect(IReceiver autoConnectedReceiver) {
            this.autoConnect = (autoConnectedReceiver != null);
            this.autoConnectedReceiver = autoConnectedReceiver;
        }

        /**
         * An own class to distinguish the class name from
         * the transmitter of other devices.
         */
        private class SequencerTransmitter : BasicTransmitter {
            internal SequencerTransmitter(RealTimeSequencer caller)
                : base(caller) {
            }
        }

        private sealed class SequencerReceiver : AbstractReceiver {
            private RealTimeSequencer caller;

            internal SequencerReceiver(RealTimeSequencer caller)
                : base(caller) {
                this.caller = caller;
            }

            internal override void implSend(MidiMessage message, long timeStamp) {
                if (caller.recording) {
                    long tickPos = 0;

                    // convert timeStamp to ticks
                    if (timeStamp < 0) {
                        tickPos = caller.getTickPosition();
                    } else {
                        lock (caller.tempoCache) {
                            tickPos = MidiUtils.microsecond2tick(caller.sequence, timeStamp, caller.tempoCache);
                        }
                    }

                    // and record to the first matching Track
                    Track track = null;
                    // do not record real-time events
                    // see 5048381: NullPointerException when saving a MIDI sequence
                    if (message.getLength() > 1) {
                        if (message is ShortMessage) {
                            ShortMessage sm = (ShortMessage)message;
                            // all real-time messages have 0xF in the high nibble of the status byte
                            if ((sm.getStatus() & 0xF0) != 0xF0) {
                                track = RecordingTrack.get(caller.recordingTracks, sm.getChannel());
                            }
                        } else {
                            // $$jb: where to record meta, sysex events?
                            // $$fb: the first recording track
                            track = RecordingTrack.get(caller.recordingTracks, -1);
                        }
                        if (track != null) {
                            // create a copy of this message
                            if (message is ShortMessage) {
                                message = new FastShortMessage((ShortMessage)message);
                            } else {
                                message = (MidiMessage)message.Clone();
                            }

                            // create new MidiEvent
                            MidiEvent me = new MidiEvent(message, tickPos);
                            track.add(me);
                        }
                    }
                }
            }
        }

        internal class RealTimeSequencerInfo : MidiDevice.Info {

            private const String name = "Real Time Sequencer";
            private const String vendor = "Oracle Corporation";
            private const String description = "Software sequencer";
            private const String version = "Version 1.0";

            internal RealTimeSequencerInfo()
                : base(name, vendor, description, version) {
            }
        } // class Info

        private class ControllerListElement {

            // $$jb: using an array for controllers b/c its
            //       easier to deal with than turning all the
            //       ints into objects to use a Vector
            internal int[] controllers;
            internal readonly IControllerEventListener listener;

            internal ControllerListElement(IControllerEventListener listener, int[] controllers) {

                this.listener = listener;
                if (controllers == null) {
                    controllers = new int[128];
                    for (int i = 0; i < 128; i++) {
                        controllers[i] = i;
                    }
                }
                this.controllers = controllers;
            }

            internal void addControllers(int[] c) {

                if (c == null) {
                    controllers = new int[128];
                    for (int i = 0; i < 128; i++) {
                        controllers[i] = i;
                    }
                    return;
                }
                int[] temp = new int[controllers.Length + c.Length];
                int elements;

                // first add what we have
                for (int i = 0; i < controllers.Length; i++) {
                    temp[i] = controllers[i];
                }
                elements = controllers.Length;
                // now add the new controllers only if we don't already have them
                for (int i = 0; i < c.Length; i++) {
                    bool flag = false;

                    for (int j = 0; j < controllers.Length; j++) {
                        if (c[i] == controllers[j]) {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag) {
                        temp[elements++] = c[i];
                    }
                }
                // now keep only the elements we need
                int[] newc = new int[elements];
                for (int i = 0; i < elements; i++) {
                    newc[i] = temp[i];
                }
                controllers = newc;
            }

            internal void removeControllers(int[] c) {

                if (c == null) {
                    controllers = new int[0];
                } else {
                    int[] temp = new int[controllers.Length];
                    int elements = 0;


                    for (int i = 0; i < controllers.Length; i++) {
                        bool flag = false;
                        for (int j = 0; j < c.Length; j++) {
                            if (controllers[i] == c[j]) {
                                flag = true;
                                break;
                            }
                        }
                        if (!flag) {
                            temp[elements++] = controllers[i];
                        }
                    }
                    // now keep only the elements remaining
                    int[] newc = new int[elements];
                    for (int i = 0; i < elements; i++) {
                        newc[i] = temp[i];
                    }
                    controllers = newc;
                }
            }

            internal int[] getControllers() {

                // return a copy of our array of controllers,
                // so others can't mess with it
                if (controllers == null) {
                    return null;
                }

                int[] c = new int[controllers.Length];

                for (int i = 0; i < controllers.Length; i++) {
                    c[i] = controllers[i];
                }
                return c;
            }
        } // class ControllerListElement

        internal class RecordingTrack {

            private readonly Track track;
            internal int channel;

            internal RecordingTrack(Track track, int channel) {
                this.track = track;
                this.channel = channel;
            }

            internal static RecordingTrack get(IList<RecordingTrack> recordingTracks, Track track) {

                lock (recordingTracks) {
                    int size = recordingTracks.Count;

                    for (int i = 0; i < size; i++) {
                        RecordingTrack current = recordingTracks[i];
                        if (current.track == track) {
                            return current;
                        }
                    }
                }
                return null;
            }

            internal static Track get(IList<RecordingTrack> recordingTracks, int channel) {

                lock (recordingTracks) {
                    int size = recordingTracks.Count;
                    for (int i = 0; i < size; i++) {
                        RecordingTrack current = recordingTracks[i];
                        if ((current.channel == channel) || (current.channel == -1)) {
                            return current.track;
                        }
                    }
                }
                return null;
            }
        }

        internal sealed class PlayThread : IRunnable {
            private RealTimeSequencer caller;
            private Thread thread;
            private readonly Object _lock = new Object();

            /** true if playback is interrupted (in close) */
            bool interrupted = false;
            bool isPumping = false;

            private readonly DataPump dataPump;

            internal PlayThread(RealTimeSequencer caller) {
                this.caller = caller;
                dataPump = new DataPump(this.caller);
                // nearly MAX_PRIORITY
                ThreadPriority priority = ThreadPriority.AboveNormal;
                //+ (((int)ThreadPriority.Highest - (int)ThreadPriority.Normal) * 3) / 4;
                thread = JSSecurityManager.createThread(this.run,
                                    "Java Sound Sequencer", // name
                                    false,                  // daemon
                                    priority,               // priority
                                    true);                  // doStart
            }

            internal DataPump getDataPump() {
                return dataPump;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void setSequence(Sequence seq) {
                dataPump.setSequence(seq);
            }

            /** start thread and pump. Requires up-to-date tempoCache */
            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void start() {
                // mark the sequencer running
                caller.running = true;

                if (!dataPump.hasCachedTempo()) {
                    long tickPos = caller.getTickPosition();
                    dataPump.setTempoMPQ(caller.tempoCache.getTempoMPQAt(tickPos));
                }
                dataPump.checkPointMillis = 0; // means restarted
                dataPump.clearNoteOnCache();
                dataPump.needReindex = true;

                dataPump.resetLoopCount();

                // notify the thread
                lock (_lock) {
                    Monitor.PulseAll(_lock);
                }
            }

            // waits until stopped
            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void stop() {
                playThreadImplStop();
                long t = Environment.TickCount;
                while (isPumping) {
                    lock (_lock) {
                        try {
                            Monitor.Wait(_lock, 2000);
                        } catch (ThreadInterruptedException) {
                            // ignore
                        }
                    }
                    // don't wait for more than 2 seconds
                    if (Environment.TickCount - t > 1900) {
                        if (Printer.err) Printer.Err("Waited more than 2 seconds in RealTimeSequencer.PlayThread.stop()!");
                        //break;
                    }
                }
            }

            internal void playThreadImplStop() {
                // mark the sequencer running
                caller.running = false;
                lock (_lock) {
                    Monitor.PulseAll(_lock);
                }
            }

            internal void close() {
                Thread oldThread = null;
                lock (this) {
                    // dispose of thread
                    interrupted = true;
                    oldThread = thread;
                    thread = null;
                }
                if (oldThread != null) {
                    // wake up the thread if it's in wait()
                    lock (_lock) {
                        Monitor.PulseAll(_lock);
                    }
                }
                // wait for the thread to terminate itself,
                // but max. 2 seconds. Must not be synchronized!
                if (oldThread != null) {
                    try {
                        oldThread.Join(2000);
                    } catch (ThreadInterruptedException) { }
                }
            }

            /**
             * Main process loop driving the media flow.
             *
             * Make sure to NOT synchronize on RealTimeSequencer
             * anywhere here (even implicit). That is a sure deadlock!
             */
            public void run() {

                while (!interrupted) {
                    bool EOM = false;
                    bool wasRunning = caller.running;
                    isPumping = !interrupted && caller.running;
                    while (!EOM && !interrupted && caller.running) {
                        EOM = dataPump.pump();

                        try {
                            Thread.Sleep(1);
                        } catch (ThreadInterruptedException) {
                            // ignore
                        }
                    }

                    playThreadImplStop();
                    if (wasRunning) {
                        dataPump.notesOff(true);
                    }
                    if (EOM) {
                        dataPump.setTickPos(caller.sequence.getTickLength());

                        // send EOT event (mis-used for end of media)
                        MetaMessage message = new MetaMessage();
                        try {
                            message.setMessage(MidiUtils.META_END_OF_TRACK_TYPE, new byte[0], 0);
                        } catch (InvalidMidiDataException) { }
                        caller.sendMetaEvents(message);
                    }
                    lock (_lock) {
                        isPumping = false;
                        // wake up a waiting stop() method
                        Monitor.PulseAll(_lock);
                        while (!caller.running && !interrupted) {
                            try {
                                Monitor.Wait(_lock);
                            } catch (Exception) { }
                        }
                    }
                } // end of while(!EOM && !interrupted && running)
            }
        }

        /**
         * class that does the actual dispatching of events,
         * used to be in native in MMAPI.
         */
        internal class DataPump {
            private RealTimeSequencer caller;

            private float currTempo;         // MPQ tempo
            private float tempoFactor;       // 1.0 is default
            private float inverseTempoFactor;// = 1.0 / tempoFactor
            private long ignoreTempoEventAt; // ignore next META tempo during playback at this tick pos only
            private int resolution;
            private float divisionType;
            internal long checkPointMillis;   // microseconds at checkoint
            private long checkPointTick;     // ticks at checkpoint
            private int[] noteOnCache;       // bit-mask of notes that are currently on
            private Track[] tracks;
            private bool[] trackDisabled; // if true, do not play this track
            private int[] trackReadPos;      // read index per track
            private long lastTick;
            internal bool needReindex = false;
            private int currLoopCounter = 0;

            //private sun.misc.Perf perf = sun.misc.Perf.getPerf();
            //private long perfFreq = perf.highResFrequency();

            internal DataPump(RealTimeSequencer caller) {
                this.caller = caller;
                init();
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void init() {
                ignoreTempoEventAt = -1;
                tempoFactor = 1.0f;
                inverseTempoFactor = 1.0f;
                noteOnCache = new int[128];
                tracks = null;
                trackDisabled = null;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void setTickPos(long tickPos) {
                long oldLastTick = tickPos;
                lastTick = tickPos;
                if (caller.running) {
                    notesOff(false);
                }
                if (caller.running || tickPos > 0) {
                    // will also reindex
                    chaseEvents(oldLastTick, tickPos);
                } else {
                    needReindex = true;
                }
                if (!hasCachedTempo()) {
                    setTempoMPQ(caller.getTempoCache().getTempoMPQAt(lastTick, currTempo));
                    // treat this as if it is a real time tempo change
                    ignoreTempoEventAt = -1;
                }
                // trigger re-configuration
                checkPointMillis = 0;
            }

            internal long getTickPos() {
                return lastTick;
            }

            // hasCachedTempo is only valid if it is the current position
            internal bool hasCachedTempo() {
                if (ignoreTempoEventAt != lastTick) {
                    ignoreTempoEventAt = -1;
                }
                return ignoreTempoEventAt >= 0;
            }

            // this method is also used internally in the pump!
            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void setTempoMPQ(float tempoMPQ) {
                if (tempoMPQ > 0 && tempoMPQ != currTempo) {
                    ignoreTempoEventAt = lastTick;
                    this.currTempo = tempoMPQ;
                    // re-calculate check point
                    checkPointMillis = 0;
                }
            }

            internal float getTempoMPQ() {
                return currTempo;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void setTempoFactor(float factor) {
                if (factor > 0 && factor != this.tempoFactor) {
                    tempoFactor = factor;
                    inverseTempoFactor = 1.0f / factor;
                    // re-calculate check point
                    checkPointMillis = 0;
                }
            }

            internal float getTempoFactor() {
                return tempoFactor;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void muteSoloChanged() {
                bool[] newDisabled = makeDisabledArray();
                if (caller.running) {
                    applyDisabledTracks(trackDisabled, newDisabled);
                }
                trackDisabled = newDisabled;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void setSequence(Sequence seq) {
                if (seq == null) {
                    init();
                    return;
                }
                tracks = seq.getTracks();
                muteSoloChanged();
                resolution = seq.getResolution();
                divisionType = seq.getDivisionType();
                trackReadPos = new int[tracks.Length];
                // trigger re-initialization
                checkPointMillis = 0;
                needReindex = true;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void resetLoopCount() {
                currLoopCounter = caller.loopCount;
            }

            internal void clearNoteOnCache() {
                for (int i = 0; i < 128; i++) {
                    noteOnCache[i] = 0;
                }
            }

            internal void notesOff(bool doControllers) {
                int done = 0;
                for (int ch = 0; ch < 16; ch++) {
                    int channelMask = (1 << ch);
                    for (int i = 0; i < 128; i++) {
                        if ((noteOnCache[i] & channelMask) != 0) {
                            noteOnCache[i] ^= channelMask;
                            // send note on with velocity 0
                            caller.getTransmitterList().sendMessage((ShortMessage.NOTE_ON | ch) | (i << 8), -1);
                            done++;
                        }
                    }
                    /* all notes off */
                    caller.getTransmitterList().sendMessage((ShortMessage.CONTROL_CHANGE | ch) | (123 << 8), -1);
                    /* sustain off */
                    caller.getTransmitterList().sendMessage((ShortMessage.CONTROL_CHANGE | ch) | (64 << 8), -1);
                    if (doControllers) {
                        /* reset all controllers */
                        caller.getTransmitterList().sendMessage((ShortMessage.CONTROL_CHANGE | ch) | (121 << 8), -1);
                        done++;
                    }
                }
            }

            private bool[] makeDisabledArray() {
                if (tracks == null) {
                    return null;
                }
                bool[] newTrackDisabled = new bool[tracks.Length];
                bool[] solo;
                bool[] mute;
                lock (caller) {
                    mute = caller.trackMuted;
                    solo = caller.trackSolo;
                }
                // if one track is solo, then only play solo
                bool hasSolo = false;
                if (solo != null) {
                    for (int i = 0; i < solo.Length; i++) {
                        if (solo[i]) {
                            hasSolo = true;
                            break;
                        }
                    }
                }
                if (hasSolo) {
                    // only the channels with solo play, regardless of mute
                    for (int i = 0; i < newTrackDisabled.Length; i++) {
                        newTrackDisabled[i] = (i >= solo.Length) || (!solo[i]);
                    }
                } else {
                    // mute the selected channels
                    for (int i = 0; i < newTrackDisabled.Length; i++) {
                        newTrackDisabled[i] = (mute != null) && (i < mute.Length) && (mute[i]);
                    }
                }
                return newTrackDisabled;
            }

            /**
             * chase all events from beginning of Track
             * and send note off for those events that are active
             * in noteOnCache array.
             * It is possible, of course, to catch notes from other tracks,
             * but better than more complicated logic to detect
             * which notes are really from this track
             */
            private void sendNoteOffIfOn(Track track, long endTick) {
                int size = track.size();
                int done = 0;
                try {
                    for (int i = 0; i < size; i++) {
                        MidiEvent evnt = track.get(i);
                        if (evnt.getTick() > endTick) break;
                        MidiMessage msg = evnt.getMessage();
                        int status = msg.getStatus();
                        int len = msg.getLength();
                        if (len == 3 && ((status & 0xF0) == ShortMessage.NOTE_ON)) {
                            int note = -1;
                            if (msg is ShortMessage) {
                                ShortMessage smsg = (ShortMessage)msg;
                                if (smsg.getData2() > 0) {
                                    // only consider Note On with velocity > 0
                                    note = smsg.getData1();
                                }
                            } else {
                                byte[] data = msg.getMessage();
                                if ((data[2] & 0x7F) > 0) {
                                    // only consider Note On with velocity > 0
                                    note = data[1] & 0x7F;
                                }
                            }
                            if (note >= 0) {
                                int bit = 1 << (status & 0x0F);
                                if ((noteOnCache[note] & bit) != 0) {
                                    // the bit is set. Send Note Off
                                    caller.getTransmitterList().sendMessage(status | (note << 8), -1);
                                    // clear the bit
                                    noteOnCache[note] &= (0xFFFF ^ bit);
                                    done++;
                                }
                            }
                        }
                    }
                } catch (IndexOutOfRangeException) {
                    // this happens when messages are removed
                    // from the track while this method executes
                }
            }

            /**
             * Runtime application of mute/solo:
             * if a track is muted that was previously playing, send
             *    note off events for all currently playing notes.
             */
            private void applyDisabledTracks(bool[] oldDisabled, bool[] newDisabled) {
                sbyte[][] tempArray = null;
                lock (caller) {
                    for (int i = 0; i < newDisabled.Length; i++) {
                        if (((oldDisabled == null)
                         || (i >= oldDisabled.Length)
                         || !oldDisabled[i])
                        && newDisabled[i]) {
                            // case that a track gets muted: need to
                            // send appropriate note off events to prevent
                            // hanging notes

                            if (tracks.Length > i) {
                                sendNoteOffIfOn(tracks[i], lastTick);
                            }
                        } else if ((oldDisabled != null)
                             && (i < oldDisabled.Length)
                             && oldDisabled[i]
                             && !newDisabled[i]) {
                            // case that a track was muted and is now unmuted
                            // need to chase events and re-index this track
                            if (tempArray == null) {
                                tempArray = new sbyte[128][];
                                for (int j = 0; j < tempArray.Length; j++) {
                                    tempArray[j] = new sbyte[16];
                                }
                            }
                            chaseTrackEvents(i, 0, lastTick, true, tempArray);
                        }
                    }
                }
            }

            /** go through all events from startTick to endTick
             * chase the controller state and program change state
             * and then set the end-states at once.
             *
             * needs to be called in synchronized state
             * @param tempArray an byte[128][16] to hold controller messages
             */
            private void chaseTrackEvents(int trackNum,
                              long startTick,
                              long endTick,
                              bool doReindex,
                              sbyte[][] tempArray) {
                if (startTick > endTick) {
                    // start from the beginning
                    startTick = 0;
                }
                sbyte[] progs = new sbyte[16];
                // init temp array with impossible values
                for (int ch = 0; ch < 16; ch++) {
                    progs[ch] = -1;
                    for (int co = 0; co < 128; co++) {
                        tempArray[co][ch] = -1;
                    }
                }
                Track track = tracks[trackNum];
                int size = track.size();
                try {
                    for (int i = 0; i < size; i++) {
                        MidiEvent evnt = track.get(i);
                        if (evnt.getTick() >= endTick) {
                            if (doReindex && (trackNum < trackReadPos.Length)) {
                                trackReadPos[trackNum] = (i > 0) ? (i - 1) : 0;
                            }
                            break;
                        }
                        MidiMessage msg = evnt.getMessage();
                        int status = msg.getStatus();
                        int len = msg.getLength();
                        if (len == 3 && ((status & 0xF0) == ShortMessage.CONTROL_CHANGE)) {
                            if (msg is ShortMessage) {
                                ShortMessage smsg = (ShortMessage)msg;
                                tempArray[smsg.getData1() & 0x7F][status & 0x0F] = (sbyte)smsg.getData2();
                            } else {
                                byte[] data = msg.getMessage();
                                tempArray[data[1] & 0x7F][status & 0x0F] = (sbyte)data[2];
                            }
                        }
                        if (len == 2 && ((status & 0xF0) == ShortMessage.PROGRAM_CHANGE)) {
                            if (msg is ShortMessage) {
                                ShortMessage smsg = (ShortMessage)msg;
                                progs[status & 0x0F] = (sbyte)smsg.getData1();
                            } else {
                                byte[] data = msg.getMessage();
                                progs[status & 0x0F] = (sbyte)data[1];
                            }
                        }
                    }
                } catch (IndexOutOfRangeException) {
                    // this happens when messages are removed
                    // from the track while this method executes
                }
                int numControllersSent = 0;
                // now send out the aggregated controllers and program changes
                for (int ch = 0; ch < 16; ch++) {
                    for (int co = 0; co < 128; co++) {
                        sbyte controllerValue = tempArray[co][ch];
                        if (controllerValue >= 0) {
                            int packedMsg = (ShortMessage.CONTROL_CHANGE | ch) | (co << 8) | (controllerValue << 16);
                            caller.getTransmitterList().sendMessage(packedMsg, -1);
                            numControllersSent++;
                        }
                    }
                    // send program change *after* controllers, to
                    // correctly initialize banks
                    if (progs[ch] >= 0) {
                        caller.getTransmitterList().sendMessage((ShortMessage.PROGRAM_CHANGE | ch) | (progs[ch] << 8), -1);
                    }
                    if (progs[ch] >= 0 || startTick == 0 || endTick == 0) {
                        // reset pitch bend on this channel (E0 00 40)
                        caller.getTransmitterList().sendMessage((ShortMessage.PITCH_BEND | ch) | (0x40 << 16), -1);
                        // reset sustain pedal on this channel
                        caller.getTransmitterList().sendMessage((ShortMessage.CONTROL_CHANGE | ch) | (64 << 8), -1);
                    }
                }
            }

            /**
             * chase controllers and program for all tracks.
             */
            [MethodImpl(MethodImplOptions.Synchronized)]
            internal void chaseEvents(long startTick, long endTick) {
                sbyte[][] tempArray = new sbyte[128][];
                for (int j = 0; j < tempArray.Length; j++) {
                    tempArray[j] = new sbyte[16];
                }
                for (int t = 0; t < tracks.Length; t++) {
                    if ((trackDisabled == null)
                        || (trackDisabled.Length <= t)
                        || (!trackDisabled[t])) {
                        // if track is not disabled, chase the events for it
                        chaseTrackEvents(t, startTick, endTick, true, tempArray);
                    }
                }
            }

            // playback related methods (pumping)

            private long getCurrentTimeMillis() {
                return Environment.TickCount;
                //return perf.highResCounter() * 1000 / perfFreq;
            }

            private long millis2tick(long millis) {
                if (divisionType != Sequence.PPQ) {
                    double dTick = ((((double)millis) * tempoFactor)
                            * ((double)divisionType)
                            * ((double)resolution))
                        / ((double)1000);
                    return (long)dTick;
                }
                return MidiUtils.microsec2ticks(millis * 1000,
                                currTempo * inverseTempoFactor,
                                resolution);
            }

            private long tick2millis(long tick) {
                if (divisionType != Sequence.PPQ) {
                    double dMillis = ((((double)tick) * 1000) /
                              (tempoFactor * ((double)divisionType) * ((double)resolution)));
                    return (long)dMillis;
                }
                return MidiUtils.ticks2microsec(tick,
                                currTempo * inverseTempoFactor,
                                resolution) / 1000;
            }

            private void ReindexTrack(int trackNum, long tick) {
                if (trackNum < trackReadPos.Length && trackNum < tracks.Length) {
                    trackReadPos[trackNum] = MidiUtils.tick2index(tracks[trackNum], tick);
                }
            }

            /* returns if changes are pending */
            private bool dispatchMessage(int trackNum, MidiEvent evnt) {
                bool changesPending = false;
                MidiMessage message = evnt.getMessage();
                int msgStatus = message.getStatus();
                int msgLen = message.getLength();
                if (msgStatus == MetaMessage.META && msgLen >= 2) {
                    // a meta message. Do not send it to the device.
                    // 0xFF with length=1 is a MIDI realtime message
                    // which shouldn't be in a Sequence, but we play it
                    // nonetheless.

                    // see if this is a tempo message. Only on track 0.
                    if (trackNum == 0) {
                        int newTempo = MidiUtils.getTempoMPQ(message);
                        if (newTempo > 0) {
                            if (evnt.getTick() != ignoreTempoEventAt) {
                                setTempoMPQ(newTempo); // sets ignoreTempoEventAt!
                                changesPending = true;
                            }
                            // next loop, do not ignore anymore tempo events.
                            ignoreTempoEventAt = -1;
                        }
                    }
                    // send to listeners
                    caller.sendMetaEvents(message);

                } else {
                    // not meta, send to device
                    caller.getTransmitterList().sendMessage(message, -1);

                    switch (msgStatus & 0xF0) {
                        case ShortMessage.NOTE_OFF: {
                                // note off - clear the bit in the noteOnCache array
                                int note = ((ShortMessage)message).getData1() & 0x7F;
                                noteOnCache[note] &= (0xFFFF ^ (1 << (msgStatus & 0x0F)));
                                break;
                            }

                        case ShortMessage.NOTE_ON: {
                                // note on
                                ShortMessage smsg = (ShortMessage)message;
                                int note = smsg.getData1() & 0x7F;
                                int vel = smsg.getData2() & 0x7F;
                                if (vel > 0) {
                                    // if velocity > 0 set the bit in the noteOnCache array
                                    noteOnCache[note] |= 1 << (msgStatus & 0x0F);
                                } else {
                                    // if velocity = 0 clear the bit in the noteOnCache array
                                    noteOnCache[note] &= (0xFFFF ^ (1 << (msgStatus & 0x0F)));
                                }
                                break;
                            }

                        case ShortMessage.CONTROL_CHANGE:
                            // if controller message, send controller listeners
                            caller.sendControllerEvents(message);
                            break;

                    }
                }
                return changesPending;
            }

            /** the main pump method
             * @return true if end of sequence is reached
             */
            [MethodImpl(MethodImplOptions.Synchronized)]
            internal bool pump() {
                long currMillis;
                long targetTick = lastTick;
                MidiEvent currEvent;
                bool changesPending = false;
                bool doLoop = false;
                bool EOM = false;

                currMillis = getCurrentTimeMillis();
                int finishedTracks = 0;
                do {
                    changesPending = false;

                    // need to re-find indexes in tracks?
                    if (needReindex) {
                        if (trackReadPos.Length < tracks.Length) {
                            trackReadPos = new int[tracks.Length];
                        }
                        for (int t = 0; t < tracks.Length; t++) {
                            ReindexTrack(t, targetTick);
                        }
                        needReindex = false;
                        checkPointMillis = 0;
                    }

                    // get target tick from current time in millis
                    if (checkPointMillis == 0) {
                        // new check point
                        currMillis = getCurrentTimeMillis();
                        checkPointMillis = currMillis;
                        targetTick = lastTick;
                        checkPointTick = targetTick;
                    } else {
                        // calculate current tick based on current time in milliseconds
                        targetTick = checkPointTick + millis2tick(currMillis - checkPointMillis);
                        if ((caller.loopEnd != -1)
                        && ((caller.loopCount > 0 && currLoopCounter > 0)
                            || (caller.loopCount == Sequencer.LOOP_CONTINUOUSLY))) {
                            if (lastTick <= caller.loopEnd && targetTick >= caller.loopEnd) {
                                // need to loop!
                                // only play until loop end
                                targetTick = caller.loopEnd - 1;
                                doLoop = true;
                            }
                        }
                        lastTick = targetTick;
                    }

                    finishedTracks = 0;

                    for (int t = 0; t < tracks.Length; t++) {
                        try {
                            bool disabled = trackDisabled[t];
                            Track thisTrack = tracks[t];
                            int readPos = trackReadPos[t];
                            int size = thisTrack.size();
                            // play all events that are due until targetTick
                            while (!changesPending && (readPos < size)
                                   && (currEvent = thisTrack.get(readPos)).getTick() <= targetTick) {

                                if ((readPos == size - 1) && MidiUtils.isMetaEndOfTrack(currEvent.getMessage())) {
                                    // do not send out this message. Finished with this track
                                    readPos = size;
                                    break;
                                }
                                // TODO: some kind of heuristics if the MIDI messages have changed
                                // significantly (i.e. deleted or inserted a bunch of messages)
                                // since last time. Would need to set needReindex = true then
                                readPos++;
                                // only play this event if the track is enabled,
                                // or if it is a tempo message on track 0
                                // Note: cannot put this check outside
                                //       this inner loop in order to detect end of file
                                if (!disabled ||
                                ((t == 0) && (MidiUtils.isMetaTempo(currEvent.getMessage())))) {
                                    changesPending = dispatchMessage(t, currEvent);
                                }
                            }
                            if (readPos >= size) {
                                finishedTracks++;
                            }
                            trackReadPos[t] = readPos;
                        } catch (Exception e) {
                            if (Printer.err) caller.printStackTrace(e);
                            if (e is IndexOutOfRangeException) {
                                needReindex = true;
                                changesPending = true;
                            }
                        }
                        if (changesPending) {
                            break;
                        }
                    }
                    EOM = (finishedTracks == tracks.Length);
                    if (doLoop
                        || (((caller.loopCount > 0 && currLoopCounter > 0)
                          || (caller.loopCount == Sequencer.LOOP_CONTINUOUSLY))
                         && !changesPending
                         && (caller.loopEnd == -1)
                         && EOM)) {

                        long oldCheckPointMillis = checkPointMillis;
                        long loopEndTick = caller.loopEnd;
                        if (loopEndTick == -1) {
                            loopEndTick = lastTick;
                        }

                        // need to loop back!
                        if (caller.loopCount != Sequencer.LOOP_CONTINUOUSLY) {
                            currLoopCounter--;
                        }
                        setTickPos(caller.loopStart);
                        // now patch the checkPointMillis so that
                        // it points to the exact beginning of when the loop was finished

                        // $$fb TODO: although this is mathematically correct (i.e. the loop position
                        //            is correct, and doesn't drift away with several repetition,
                        //            there is a slight lag when looping back, probably caused
                        //            by the chasing.

                        checkPointMillis = oldCheckPointMillis + tick2millis(loopEndTick - checkPointTick);
                        checkPointTick = caller.loopStart;
                        // no need for reindexing, is done in setTickPos
                        needReindex = false;
                        changesPending = false;
                        // reset doLoop flag
                        doLoop = false;
                        EOM = false;
                    }
                } while (changesPending);

                return EOM;
            }

        } // class DataPump

        private void printStackTrace(Exception ex) {
            Printer.printStackTrace(ex);
        }
    }
}
