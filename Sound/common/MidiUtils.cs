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

//import java.util.ArrayList;

//import javax.sound.midi.InvalidMidiDataException;
//import javax.sound.midi.MetaMessage;
//import javax.sound.midi.MidiDevice;
//import javax.sound.midi.MidiEvent;
//import javax.sound.midi.MidiMessage;
//import javax.sound.midi.Sequence;
//import javax.sound.midi.Track;

//import static javax.sound.midi.SysexMessage.SPECIAL_SYSTEM_EXCLUSIVE;
//import static javax.sound.midi.SysexMessage.SYSTEM_EXCLUSIVE;

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {
// TODO:
// - define and use a global symbolic constant for 60000000 (see convertTempo)

/**
 * Some utilities for MIDI (some stuff is used from javax.sound.midi)
 *
 * @author Florian Bomers
 */
    public sealed class MidiUtils {

        public const int DEFAULT_TEMPO_MPQ = 500000; // 120bpm
        public const int META_END_OF_TRACK_TYPE = 0x2F;
        public const int META_TEMPO_TYPE = 0x51;

        /**
         * Suppresses default constructor, ensuring non-instantiability.
         */
        private MidiUtils() {
        }

        /**
         * Returns an exception which should be thrown if MidiDevice is unsupported.
         *
         * @param  info an info object that describes the desired device
         * @return an exception instance
         */
        internal static ArgumentException unsupportedDevice(MidiDevice.Info info) {
            return new ArgumentException(String.Format(
                    "MidiDevice {0} not supported by this provider", info));
        }

        /**
         * Checks the status byte for the system exclusive message.
         *
         * @param  data the system exclusive message data
         * @param  length the length of the valid message data in the array
         * @throws InvalidMidiDataException if the status byte is invalid for a
         *         system exclusive message
         */
        public static void checkSysexStatus(byte[] data, int length) {
            if (data.Length == 0 || length == 0) {
                throw new InvalidMidiDataException("Status byte is missing");
            }
            checkSysexStatus(data[0] & 0xFF);
        }

        /**
         * Checks the status byte for the system exclusive message.
         *
         * @param  status the status byte for the message (0xF0 or 0xF7)
         * @throws InvalidMidiDataException if the status byte is invalid for a
         *         system exclusive message
         */
        public static void checkSysexStatus(int status) {
            if (status != SysexMessage.SYSTEM_EXCLUSIVE && status != SysexMessage.SPECIAL_SYSTEM_EXCLUSIVE) {
                throw new InvalidMidiDataException(String.Format(
                        "Invalid status byte for sysex message: 0x{0:X}", status));
            }
        }

        /** return true if the passed message is Meta End Of Track */
        public static bool isMetaEndOfTrack(MidiMessage midiMsg) {
            // first check if it is a META message at all
            if (midiMsg.getLength() != 3
                || midiMsg.getStatus() != MetaMessage.META) {
                return false;
            }
            // now get message and check for end of track
            byte[] msg = midiMsg.getMessage();
            return ((msg[1] & 0xFF) == META_END_OF_TRACK_TYPE) && (msg[2] == 0);
        }

        /** return if the given message is a meta tempo message */
        public static bool isMetaTempo(MidiMessage midiMsg) {
            // first check if it is a META message at all
            if (midiMsg.getLength() != 6
                || midiMsg.getStatus() != MetaMessage.META) {
                return false;
            }
            // now get message and check for tempo
            byte[] msg = midiMsg.getMessage();
            // meta type must be 0x51, and data length must be 3
            return ((msg[1] & 0xFF) == META_TEMPO_TYPE) && (msg[2] == 3);
        }

        /** parses this message for a META tempo message and returns
         * the tempo in MPQ, or -1 if this isn't a tempo message
         */
        public static int getTempoMPQ(MidiMessage midiMsg) {
            // first check if it is a META message at all
            if (midiMsg.getLength() != 6
                || midiMsg.getStatus() != MetaMessage.META) {
                return -1;
            }
            byte[] msg = midiMsg.getMessage();
            if (((msg[1] & 0xFF) != META_TEMPO_TYPE) || (msg[2] != 3)) {
                return -1;
            }
            int tempo = (msg[5] & 0xFF)
                        | ((msg[4] & 0xFF) << 8)
                        | ((msg[3] & 0xFF) << 16);
            return tempo;
        }

        /**
         * converts<br>
         * 1 - MPQ-Tempo to BPM tempo<br>
         * 2 - BPM tempo to MPQ tempo<br>
         */
        public static double convertTempo(double tempo) {
            if (tempo <= 0) {
                tempo = 1;
            }
            return ((double)60000000L) / tempo;
        }

        /**
         * convert tick to microsecond with given tempo.
         * Does not take tempo changes into account.
         * Does not work for SMPTE timing!
         */
        public static long ticks2microsec(long tick, double tempoMPQ, int resolution) {
            return (long)(((double)tick) * tempoMPQ / resolution);
        }

        /**
         * convert tempo to microsecond with given tempo
         * Does not take tempo changes into account.
         * Does not work for SMPTE timing!
         */
        public static long microsec2ticks(long us, double tempoMPQ, int resolution) {
            // do not round to nearest tick
            //return (long) Math.round((((double)us) * resolution) / tempoMPQ);
            return (long)((((double)us) * resolution) / tempoMPQ);
        }

        /**
         * Given a tick, convert to microsecond
         * @param cache tempo info and current tempo
         */
        public static long tick2microsecond(Sequence seq, long tick, TempoCache cache) {
            if (seq.getDivisionType() != Sequence.PPQ) {
                double seconds = ((double)tick / (double)(seq.getDivisionType() * seq.getResolution()));
                return (long)(1000000 * seconds);
            }

            if (cache == null) {
                cache = new TempoCache(seq);
            }

            int resolution = seq.getResolution();

            long[] ticks = cache.ticks;
            int[] tempos = cache.tempos; // in MPQ
            int cacheCount = tempos.Length;

            // optimization to not always go through entire list of tempo events
            int snapshotIndex = cache.snapshotIndex;
            int snapshotMicro = cache.snapshotMicro;

            // walk through all tempo changes and add time for the respective blocks
            long us = 0; // microsecond

            if (snapshotIndex <= 0
                || snapshotIndex >= cacheCount
                || ticks[snapshotIndex] > tick) {
                snapshotMicro = 0;
                snapshotIndex = 0;
            }
            if (cacheCount > 0) {
                // this implementation needs a tempo event at tick 0!
                int i = snapshotIndex + 1;
                while (i < cacheCount && ticks[i] <= tick) {
                    snapshotMicro += (int)ticks2microsec(ticks[i] - ticks[i - 1], tempos[i - 1], resolution);
                    snapshotIndex = i;
                    i++;
                }
                us = snapshotMicro
                + ticks2microsec(tick - ticks[snapshotIndex],
                         tempos[snapshotIndex],
                         resolution);
            }
            cache.snapshotIndex = snapshotIndex;
            cache.snapshotMicro = snapshotMicro;
            return us;
        }

        /**
         * Given a microsecond time, convert to tick.
         * returns tempo at the given time in cache.getCurrTempoMPQ
         */
        public static long microsecond2tick(Sequence seq, long micros, TempoCache cache) {
            long tick;
            if (seq.getDivisionType() != Sequence.PPQ) {
                double dTick = (((double)micros)
                               * ((double)seq.getDivisionType())
                               * ((double)seq.getResolution()))
                               / ((double)1000000);
                tick = (long)dTick;
                if (cache != null) {
                    cache.currTempo = (int)cache.getTempoMPQAt(tick);
                }
                return tick;
            }

            if (cache == null) {
                cache = new TempoCache(seq);
            }
            long[] ticks = cache.ticks;
            int[] tempos = cache.tempos; // in MPQ
            int cacheCount = tempos.Length;

            int resolution = seq.getResolution();

            long us = 0; tick = 0;
#pragma warning disable 0219
            int newReadPos = 0;
#pragma warning restore 0219
            int i = 1;

            // walk through all tempo changes and add time for the respective blocks
            // to find the right tick
            if (micros > 0 && cacheCount > 0) {
                // this loop requires that the first tempo Event is at time 0
                while (i < cacheCount) {
                    long nextTime = us + ticks2microsec(ticks[i] - ticks[i - 1],
                                        tempos[i - 1], resolution);
                    if (nextTime > micros) {
                        break;
                    }
                    us = nextTime;
                    i++;
                }
                tick = ticks[i - 1] + microsec2ticks(micros - us, tempos[i - 1], resolution);
            }
            cache.currTempo = tempos[i - 1];
            return tick;
        }

        /**
         * Binary search for the event indexes of the track
         *
         * @param tick  tick number of index to be found in array
         * @return index in track which is on or after "tick".
         *   if no entries are found that follow after tick, track.size() is returned
         */
        public static int tick2index(Track track, long tick) {
            int ret = 0;
            if (tick > 0) {
                int low = 0;
                int high = track.size() - 1;
                while (low < high) {
                    // take the middle event as estimate
                    ret = (low + high) >> 1;
                    // tick of estimate
                    long t = track.get(ret).getTick();
                    if (t == tick) {
                        break;
                    } else if (t < tick) {
                        // estimate too low
                        if (low == high - 1) {
                            // "or after tick"
                            ret++;
                            break;
                        }
                        low = ret;
                    } else { // if (t>tick)
                        // estimate too high
                        high = ret;
                    }
                }
            }
            return ret;
        }

        public sealed class TempoCache {
            internal long[] ticks;
            internal int[] tempos; // in MPQ
            // index in ticks/tempos at the snapshot
            internal int snapshotIndex = 0;
            // microsecond at the snapshot
            internal int snapshotMicro = 0;

            internal int currTempo; // MPQ, used as return value for microsecond2tick

            private bool firstTempoIsFake = false;

            public TempoCache() {
                // just some defaults, to prevents weird stuff
                ticks = new long[1];
                tempos = new int[1];
                tempos[0] = DEFAULT_TEMPO_MPQ;
                snapshotIndex = 0;
                snapshotMicro = 0;
            }

            public TempoCache(Sequence seq)
                : this() {
                refresh(seq);
            }


            [MethodImpl(MethodImplOptions.Synchronized)]
            public void refresh(Sequence seq) {
                List<MidiEvent> list = new List<MidiEvent>();
                Track[] tracks = seq.getTracks();
                if (tracks.Length > 0) {
                    // tempo events only occur in track 0
                    Track track = tracks[0];
                    int c = track.size();
                    for (int i = 0; i < c; i++) {
                        MidiEvent ev = track.get(i);
                        MidiMessage msg = ev.getMessage();
                        if (isMetaTempo(msg)) {
                            // found a tempo event. Add it to the list
                            list.Add(ev);
                        }
                    }
                }
                int size = list.Count + 1;
                firstTempoIsFake = true;
                if ((size > 1)
                && (list[0].getTick() == 0)) {
                    // do not need to add an initial tempo event at the beginning
                    size--;
                    firstTempoIsFake = false;
                }
                ticks = new long[size];
                tempos = new int[size];
                int e = 0;
                if (firstTempoIsFake) {
                    // add tempo 120 at beginning
                    ticks[0] = 0;
                    tempos[0] = DEFAULT_TEMPO_MPQ;
                    e++;
                }
                for (int i = 0; i < list.Count; i++, e++) {
                    MidiEvent evt = list[i];
                    ticks[e] = evt.getTick();
                    tempos[e] = getTempoMPQ(evt.getMessage());
                }
                snapshotIndex = 0;
                snapshotMicro = 0;
            }

            public int getCurrTempoMPQ() {
                return currTempo;
            }

            public float getTempoMPQAt(long tick) {
                return getTempoMPQAt(tick, -1.0f);
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public float getTempoMPQAt(long tick, float startTempoMPQ) {
                for (int i = 0; i < ticks.Length; i++) {
                    if (ticks[i] > tick) {
                        if (i > 0) i--;
                        if (startTempoMPQ > 0 && i == 0 && firstTempoIsFake) {
                            return startTempoMPQ;
                        }
                        return (float)tempos[i];
                    }
                }
                return tempos[tempos.Length - 1];
            }
        }
    }
}
