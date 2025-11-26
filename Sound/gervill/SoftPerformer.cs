/*
 * Copyright (c) 2007, 2023, Oracle and/or its affiliates. All rights reserved.
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
//import java.util.Arrays;
//import java.util.Comparator;
//import java.util.HashMap;
//import java.util.List;
//import java.util.Map;

using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SystemX.Media.Sound {

/**
 * This class decodes information from ModelPerformer for use in SoftVoice.
 * It also adds default connections if they where missing in ModelPerformer.
 *
 * @author Karl Helgason
 */
    public sealed class SoftPerformer {

        static ModelConnectionBlock[] defaultconnections
                = new ModelConnectionBlock[42];

        static SoftPerformer() {
            int o = 0;
            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("noteon", "on", 0),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                1, new ModelDestination(new ModelIdentifier("eg", "on", 0)));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("noteon", "on", 0),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                1, new ModelDestination(new ModelIdentifier("eg", "on", 1)));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("eg", "active", 0),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                1, new ModelDestination(new ModelIdentifier("mixer", "active", 0)));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("eg", 0),
                    ModelStandardTransform.DIRECTION_MAX2MIN,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                -960, new ModelDestination(new ModelIdentifier("mixer", "gain")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("noteon", "velocity"),
                    ModelStandardTransform.DIRECTION_MAX2MIN,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_CONCAVE),
                -960, new ModelDestination(new ModelIdentifier("mixer", "gain")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi", "pitch"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                new ModelSource(new ModelIdentifier("midi_rpn", "0"),
                    new ModelTransformImpl0()), // {
                //    public double transform(double value) {
                //        int v = (int) (value * 16384.0);
                //        int msb = v >> 7;
                //        int lsb = v & 127;
                //        return msb * 100 + lsb;
                //    }
                //}),
                new ModelDestination(new ModelIdentifier("osc", "pitch")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("noteon", "keynumber"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                12800, new ModelDestination(new ModelIdentifier("osc", "pitch")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "7"),
                    ModelStandardTransform.DIRECTION_MAX2MIN,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_CONCAVE),
                -960, new ModelDestination(new ModelIdentifier("mixer", "gain")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "8"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                1000, new ModelDestination(new ModelIdentifier("mixer", "balance")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "10"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                1000, new ModelDestination(new ModelIdentifier("mixer", "pan")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "11"),
                    ModelStandardTransform.DIRECTION_MAX2MIN,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_CONCAVE),
                -960, new ModelDestination(new ModelIdentifier("mixer", "gain")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "91"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                1000, new ModelDestination(new ModelIdentifier("mixer", "reverb")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "93"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                1000, new ModelDestination(new ModelIdentifier("mixer", "chorus")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "71"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                200, new ModelDestination(new ModelIdentifier("filter", "q")));
            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "74"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                9600, new ModelDestination(new ModelIdentifier("filter", "freq")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "72"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                6000, new ModelDestination(new ModelIdentifier("eg", "release2")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "73"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                2000, new ModelDestination(new ModelIdentifier("eg", "attack2")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "75"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                6000, new ModelDestination(new ModelIdentifier("eg", "decay2")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "67"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_SWITCH),
                -50, new ModelDestination(ModelDestination.DESTINATION_GAIN));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_cc", "67"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_UNIPOLAR,
                    ModelStandardTransform.TRANSFORM_SWITCH),
                -2400, new ModelDestination(ModelDestination.DESTINATION_FILTER_FREQ));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_rpn", "1"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                100, new ModelDestination(new ModelIdentifier("osc", "pitch")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("midi_rpn", "2"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                12800, new ModelDestination(new ModelIdentifier("osc", "pitch")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("master", "fine_tuning"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                100, new ModelDestination(new ModelIdentifier("osc", "pitch")));

            defaultconnections[o++] = new ModelConnectionBlock(
                new ModelSource(
                    new ModelIdentifier("master", "coarse_tuning"),
                    ModelStandardTransform.DIRECTION_MIN2MAX,
                    ModelStandardTransform.POLARITY_BIPOLAR,
                    ModelStandardTransform.TRANSFORM_LINEAR),
                12800, new ModelDestination(new ModelIdentifier("osc", "pitch")));

            defaultconnections[o++] = new ModelConnectionBlock(13500,
                    new ModelDestination(new ModelIdentifier("filter", "freq", 0)));

            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "delay", 0)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "attack", 0)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "hold", 0)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "decay", 0)));
            defaultconnections[o++] = new ModelConnectionBlock(1000,
                    new ModelDestination(new ModelIdentifier("eg", "sustain", 0)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "release", 0)));
            defaultconnections[o++] = new ModelConnectionBlock(1200.0
                    * Math.Log(0.015) / Math.Log(2), new ModelDestination(
                    new ModelIdentifier("eg", "shutdown", 0))); // 15 msec default

            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "delay", 1)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "attack", 1)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "hold", 1)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "decay", 1)));
            defaultconnections[o++] = new ModelConnectionBlock(1000,
                    new ModelDestination(new ModelIdentifier("eg", "sustain", 1)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("eg", "release", 1)));

            defaultconnections[o++] = new ModelConnectionBlock(-8.51318,
                    new ModelDestination(new ModelIdentifier("lfo", "freq", 0)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("lfo", "delay", 0)));
            defaultconnections[o++] = new ModelConnectionBlock(-8.51318,
                    new ModelDestination(new ModelIdentifier("lfo", "freq", 1)));
            defaultconnections[o++] = new ModelConnectionBlock(
                    Single.NegativeInfinity, new ModelDestination(
                    new ModelIdentifier("lfo", "delay", 1)));

        }
        public int keyFrom = 0;
        public int keyTo = 127;
        public int velFrom = 0;
        public int velTo = 127;
        public int exclusiveClass = 0;
        public bool selfNonExclusive = false;
        public bool forcedVelocity = false;
        public bool forcedKeynumber = false;
        public ModelPerformer performer;
        public ModelConnectionBlock[] connections;
        public IModelOscillator[] oscillators;
        public IDictionary<Int32, int[]> midi_rpn_connections = new Dictionary<Int32, int[]>();
        public IDictionary<Int32, int[]> midi_nrpn_connections = new Dictionary<Int32, int[]>();
        public int[][] midi_ctrl_connections;
        public int[][] midi_connections;
        public int[] ctrl_connections;
        private readonly List<Int32> ctrl_connections_list = new List<Int32>();

        private class KeySortComparator : IComparer<ModelSource> {

            public int Compare(ModelSource o1, ModelSource o2) {
                //return o1.getIdentifier().ToString().CompareTo(
                //        o2.getIdentifier().ToString());
                return String.Compare(o1.getIdentifier().ToString(), o2.getIdentifier().ToString(), StringComparison.Ordinal);
            }
        }

        private static readonly KeySortComparator keySortComparator = new KeySortComparator();

        private String extractKeys(ModelConnectionBlock conn) {
            StringBuilder sb = new StringBuilder();
            if (conn.getSources() != null) {
                sb.Append("[");
                ModelSource[] srcs = conn.getSources();
                ModelSource[] srcs2 = new ModelSource[srcs.Length];
                for (int i = 0; i < srcs.Length; i++)
                    srcs2[i] = srcs[i];
                Array.Sort(srcs2, keySortComparator);
                for (int i = 0; i < srcs.Length; i++) {
                    sb.Append(srcs[i].getIdentifier());
                    sb.Append(";");
                }
                sb.Append("]");
            }
            sb.Append(";");
            if (conn.getDestination() != null) {
                sb.Append(conn.getDestination().getIdentifier());
            }
            sb.Append(";");
            return sb.ToString();
        }

        private void processSource(ModelSource src, int ix) {
            ModelIdentifier id = src.getIdentifier();
            String o = id.getObject();
            if (o.Equals("midi_cc"))
                processMidiControlSource(src, ix);
            else if (o.Equals("midi_rpn"))
                processMidiRpnSource(src, ix);
            else if (o.Equals("midi_nrpn"))
                processMidiNrpnSource(src, ix);
            else if (o.Equals("midi"))
                processMidiSource(src, ix);
            else if (o.Equals("noteon"))
                processNoteOnSource(src, ix);
            else if (o.Equals("osc"))
                return;
            else if (o.Equals("mixer"))
                return;
            else
                ctrl_connections_list.Add(ix);
        }

        private void processMidiControlSource(ModelSource src, int ix) {
            String v = src.getIdentifier().getVariable();
            if (v == null)
                return;
            int c = Int32.Parse(v, NumberFormatInfo.InvariantInfo);
            if (midi_ctrl_connections[c] == null)
                midi_ctrl_connections[c] = new int[] { ix };
            else {
                int[] olda = midi_ctrl_connections[c];
                int[] newa = new int[olda.Length + 1];
                for (int i = 0; i < olda.Length; i++)
                    newa[i] = olda[i];
                newa[newa.Length - 1] = ix;
                midi_ctrl_connections[c] = newa;
            }
        }

        private void processNoteOnSource(ModelSource src, int ix) {
            String v = src.getIdentifier().getVariable();
            int c = -1;
            if (v.Equals("on"))
                c = 3;
            if (v.Equals("keynumber"))
                c = 4;
            if (c == -1)
                return;
            if (midi_connections[c] == null)
                midi_connections[c] = new int[] { ix };
            else {
                int[] olda = midi_connections[c];
                int[] newa = new int[olda.Length + 1];
                for (int i = 0; i < olda.Length; i++)
                    newa[i] = olda[i];
                newa[newa.Length - 1] = ix;
                midi_connections[c] = newa;
            }
        }

        private void processMidiSource(ModelSource src, int ix) {
            String v = src.getIdentifier().getVariable();
            int c = -1;
            if (v.Equals("pitch"))
                c = 0;
            if (v.Equals("channel_pressure"))
                c = 1;
            if (v.Equals("poly_pressure"))
                c = 2;
            if (c == -1)
                return;
            if (midi_connections[c] == null)
                midi_connections[c] = new int[] { ix };
            else {
                int[] olda = midi_connections[c];
                int[] newa = new int[olda.Length + 1];
                for (int i = 0; i < olda.Length; i++)
                    newa[i] = olda[i];
                newa[newa.Length - 1] = ix;
                midi_connections[c] = newa;
            }
        }

        private void processMidiRpnSource(ModelSource src, int ix) {
            String v = src.getIdentifier().getVariable();
            if (v == null)
                return;
            int c = Int32.Parse(v, NumberFormatInfo.InvariantInfo);
            if (!midi_rpn_connections.ContainsKey(c))
                midi_rpn_connections[c] = new int[] { ix };
            else {
                int[] olda = midi_rpn_connections[c];
                int[] newa = new int[olda.Length + 1];
                for (int i = 0; i < olda.Length; i++)
                    newa[i] = olda[i];
                newa[newa.Length - 1] = ix;
                midi_rpn_connections[c] = newa;
            }
        }

        private void processMidiNrpnSource(ModelSource src, int ix) {
            String v = src.getIdentifier().getVariable();
            if (v == null)
                return;
            int c = Int32.Parse(v, NumberFormatInfo.InvariantInfo);
            if (!midi_nrpn_connections.ContainsKey(c))
                midi_nrpn_connections[c] = new int[] { ix };
            else {
                int[] olda = midi_nrpn_connections[c];
                int[] newa = new int[olda.Length + 1];
                for (int i = 0; i < olda.Length; i++)
                    newa[i] = olda[i];
                newa[newa.Length - 1] = ix;
                midi_nrpn_connections[c] = newa;
            }
        }

        public SoftPerformer(ModelPerformer performer) {
            ModelConnectionBlock connection;
            this.performer = performer;

            keyFrom = performer.getKeyFrom();
            keyTo = performer.getKeyTo();
            velFrom = performer.getVelFrom();
            velTo = performer.getVelTo();
            exclusiveClass = performer.getExclusiveClass();
            selfNonExclusive = performer.isSelfNonExclusive();

            Dictionary<String, ModelConnectionBlock> connmap = new Dictionary<String, ModelConnectionBlock>();

            List<ModelConnectionBlock> performer_connections = new List<ModelConnectionBlock>();
            performer_connections.AddRange(performer.getConnectionBlocks());

            if (performer.isDefaultConnectionsEnabled()) {

                // Add modulation depth range (RPN 5) to the modulation wheel (cc#1)

                bool isModulationWheelConectionFound = false;
                for (int j = 0; j < performer_connections.Count; j++) {
                    connection = performer_connections[j];
                    ModelSource[] sources = connection.getSources();
                    ModelDestination dest = connection.getDestination();
                    bool isModulationWheelConection = false;
                    if (dest != null && sources != null && sources.Length > 1) {
                        for (int i = 0; i < sources.Length; i++) {
                            // check if connection block has the source "modulation
                            // wheel cc#1"
                            if (sources[i].getIdentifier().getObject().Equals(
                                    "midi_cc")) {
                                if (sources[i].getIdentifier().getVariable()
                                        .Equals("1")) {
                                    isModulationWheelConection = true;
                                    isModulationWheelConectionFound = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (isModulationWheelConection) {

                        ModelConnectionBlock newconnection = new ModelConnectionBlock();
                        newconnection.setSources(connection.getSources());
                        newconnection.setDestination(connection.getDestination());
                        newconnection.addSource(new ModelSource(
                                new ModelIdentifier("midi_rpn", "5")));
                        newconnection.setScale(connection.getScale() * 256.0);
                        performer_connections[j] = newconnection;
                    }
                }

                if (!isModulationWheelConectionFound) {
                    ModelConnectionBlock conn = new ModelConnectionBlock(
                            new ModelSource(ModelSource.SOURCE_LFO1,
                            ModelStandardTransform.DIRECTION_MIN2MAX,
                            ModelStandardTransform.POLARITY_BIPOLAR,
                            ModelStandardTransform.TRANSFORM_LINEAR),
                            new ModelSource(new ModelIdentifier("midi_cc", "1", 0),
                            ModelStandardTransform.DIRECTION_MIN2MAX,
                            ModelStandardTransform.POLARITY_UNIPOLAR,
                            ModelStandardTransform.TRANSFORM_LINEAR),
                            50,
                            new ModelDestination(ModelDestination.DESTINATION_PITCH));
                    conn.addSource(new ModelSource(new ModelIdentifier("midi_rpn",
                            "5")));
                    conn.setScale(conn.getScale() * 256.0);
                    performer_connections.Add(conn);

                }

                // Let Aftertouch to behave just like modulation wheel (cc#1)
                bool channel_pressure_set = false;
                bool poly_pressure = false;
                ModelConnectionBlock mod_cc_1_connection = null;
                int mod_cc_1_connection_src_ix = 0;

                foreach (ModelConnectionBlock connection0 in performer_connections) {
                    ModelSource[] sources = connection0.getSources();
                    ModelDestination dest = connection0.getDestination();
                    // if(dest != null && sources != null)
                    if (dest != null && sources != null) {
                        for (int i = 0; i < sources.Length; i++) {
                            ModelIdentifier srcid = sources[i].getIdentifier();
                            // check if connection block has the source "modulation
                            // wheel cc#1"
                            if (srcid.getObject().Equals("midi_cc")) {
                                if (srcid.getVariable().Equals("1")) {
                                    mod_cc_1_connection = connection0;
                                    mod_cc_1_connection_src_ix = i;
                                }
                            }
                            // check if channel or poly pressure are already
                            // connected
                            if (srcid.getObject().Equals("midi")) {
                                if (srcid.getVariable().Equals("channel_pressure"))
                                    channel_pressure_set = true;
                                if (srcid.getVariable().Equals("poly_pressure"))
                                    poly_pressure = true;
                            }
                        }
                    }

                }

                if (mod_cc_1_connection != null) {
                    if (!channel_pressure_set) {
                        ModelConnectionBlock mc = new ModelConnectionBlock();
                        mc.setDestination(mod_cc_1_connection.getDestination());
                        mc.setScale(mod_cc_1_connection.getScale());
                        ModelSource[] src_list = mod_cc_1_connection.getSources();
                        ModelSource[] src_list_new = new ModelSource[src_list.Length];
                        for (int i = 0; i < src_list_new.Length; i++)
                            src_list_new[i] = src_list[i];
                        src_list_new[mod_cc_1_connection_src_ix] = new ModelSource(
                                new ModelIdentifier("midi", "channel_pressure"));
                        mc.setSources(src_list_new);
                        connmap[extractKeys(mc)] = mc;
                    }
                    if (!poly_pressure) {
                        ModelConnectionBlock mc = new ModelConnectionBlock();
                        mc.setDestination(mod_cc_1_connection.getDestination());
                        mc.setScale(mod_cc_1_connection.getScale());
                        ModelSource[] src_list = mod_cc_1_connection.getSources();
                        ModelSource[] src_list_new = new ModelSource[src_list.Length];
                        for (int i = 0; i < src_list_new.Length; i++)
                            src_list_new[i] = src_list[i];
                        src_list_new[mod_cc_1_connection_src_ix] = new ModelSource(
                                new ModelIdentifier("midi", "poly_pressure"));
                        mc.setSources(src_list_new);
                        connmap[extractKeys(mc)] = mc;
                    }
                }

                // Enable Vibration Sound Controllers : 76, 77, 78
                ModelConnectionBlock found_vib_connection = null;
                foreach (ModelConnectionBlock connection0 in performer_connections) {
                    ModelSource[] sources = connection0.getSources();
                    if (sources.Length != 0
                            && sources[0].getIdentifier().getObject().Equals("lfo")) {
                        if (connection0.getDestination().getIdentifier().Equals(
                                ModelDestination.DESTINATION_PITCH)) {
                            if (found_vib_connection == null)
                                found_vib_connection = connection0;
                            else {
                                if (found_vib_connection.getSources().Length > sources.Length)
                                    found_vib_connection = connection0;
                                else if (found_vib_connection.getSources()[0]
                                        .getIdentifier().getInstance() < 1) {
                                    if (found_vib_connection.getSources()[0]
                                            .getIdentifier().getInstance() >
                                            sources[0].getIdentifier().getInstance()) {
                                        found_vib_connection = connection0;
                                    }
                                }
                            }

                        }
                    }
                }

                int instance = 1;

                if (found_vib_connection != null) {
                    instance = found_vib_connection.getSources()[0].getIdentifier()
                            .getInstance();
                }


                connection = new ModelConnectionBlock(
                    new ModelSource(new ModelIdentifier("midi_cc", "78"),
                        ModelStandardTransform.DIRECTION_MIN2MAX,
                        ModelStandardTransform.POLARITY_BIPOLAR,
                        ModelStandardTransform.TRANSFORM_LINEAR),
                    2000, new ModelDestination(
                        new ModelIdentifier("lfo", "delay2", instance)));
                connmap[extractKeys(connection)] = connection;

                double scale = found_vib_connection == null ? 0
                        : found_vib_connection.getScale();
                connection = new ModelConnectionBlock(
                    new ModelSource(new ModelIdentifier("lfo", instance)),
                    new ModelSource(new ModelIdentifier("midi_cc", "77"),
                        new ModelTransformImpl1(scale)
                    //double s = scale;
                    //public double transform(double value) {
                    //    value = value * 2 - 1;
                    //    value *= 600;
                    //    if (s == 0) {
                    //        return value;
                    //    } else if (s > 0) {
                    //        if (value < -s)
                    //            value = -s;
                    //        return value;
                    //    } else {
                    //        if (value < s)
                    //            value = -s;
                    //        return -value;
                    //    }
                    //}
                        ),
                        new ModelDestination(ModelDestination.DESTINATION_PITCH));
                connmap[extractKeys(connection)] = connection;

                connection = new ModelConnectionBlock(
                    new ModelSource(new ModelIdentifier("midi_cc", "76"),
                        ModelStandardTransform.DIRECTION_MIN2MAX,
                        ModelStandardTransform.POLARITY_BIPOLAR,
                        ModelStandardTransform.TRANSFORM_LINEAR),
                    2400, new ModelDestination(
                        new ModelIdentifier("lfo", "freq", instance)));
                connmap[extractKeys(connection)] = connection;

            }

            // Add default connection blocks
            if (performer.isDefaultConnectionsEnabled())
                foreach (ModelConnectionBlock connection0 in defaultconnections)
                    connmap[extractKeys(connection0)] = connection0;
            // Add connection blocks from modelperformer
            foreach (ModelConnectionBlock connection0 in performer_connections)
                connmap[extractKeys(connection0)] = connection0;
            // separate connection blocks : Init time, Midi Time, Midi/Control Time,
            // Control Time
            List<ModelConnectionBlock> connections = new List<ModelConnectionBlock>();

            midi_ctrl_connections = new int[128][];
            midi_connections = new int[5][];

            int ix = 0;
            bool mustBeOnTop = false;

            foreach (ModelConnectionBlock connection0 in connmap.Values) {
                if (connection0.getDestination() != null) {
                    ModelDestination dest = connection0.getDestination();
                    ModelIdentifier id = dest.getIdentifier();
                    if (id.getObject().Equals("noteon")) {
                        mustBeOnTop = true;
                        if (id.getVariable().Equals("keynumber"))
                            forcedKeynumber = true;
                        if (id.getVariable().Equals("velocity"))
                            forcedVelocity = true;
                    }
                }
                if (mustBeOnTop) {
                    connections.Insert(0, connection0);
                    mustBeOnTop = false;
                } else
                    connections.Add(connection0);
            }

            foreach (ModelConnectionBlock connection0 in connections) {
                if (connection0.getSources() != null) {
                    ModelSource[] srcs = connection0.getSources();
                    for (int i = 0; i < srcs.Length; i++) {
                        processSource(srcs[i], ix);
                    }
                }
                ix++;
            }

            this.connections = new ModelConnectionBlock[connections.Count];
            this.connections = connections.ToArray();

            this.ctrl_connections = new int[ctrl_connections_list.Count];

            for (int i = 0; i < this.ctrl_connections.Length; i++)
                this.ctrl_connections[i] = ctrl_connections_list[i];

            oscillators = new IModelOscillator[performer.getOscillators().Count];
            oscillators = ((List<IModelOscillator>)performer.getOscillators()).ToArray();

            foreach (ModelConnectionBlock conn in connections) {
                if (conn.getDestination() != null) {
                    if (isUnnecessaryTransform(conn.getDestination().getTransform())) {
                        conn.getDestination().setTransform(null);
                    }
                }
                if (conn.getSources() != null) {
                    foreach (ModelSource src in conn.getSources()) {
                        if (isUnnecessaryTransform(src.getTransform())) {
                            src.setTransform(null);
                        }
                    }
                }
            }

        }

        private static bool isUnnecessaryTransform(IModelTransform transform) {
            if (transform == null)
                return false;
            if (!(transform is ModelStandardTransform))
                return false;
            ModelStandardTransform stransform = (ModelStandardTransform)transform;
            if (stransform.getDirection() != ModelStandardTransform.DIRECTION_MIN2MAX)
                return false;
            if (stransform.getPolarity() != ModelStandardTransform.POLARITY_UNIPOLAR)
                return false;
            if (stransform.getTransform() != ModelStandardTransform.TRANSFORM_LINEAR)
                return false;
            return false;
        }

        private class ModelTransformImpl0 : IModelTransform {
            public double transform(double value) {
                int v = (int)(value * 16384.0);
                int msb = v >> 7;
                int lsb = v & 127;
                return msb * 100 + lsb;
            }
        }

        private class ModelTransformImpl1 : IModelTransform {
            double s; // = scale;
            public ModelTransformImpl1(double scale) {
                s = scale;
            }

            public double transform(double value) {
                value = value * 2 - 1;
                value *= 600;
                if (s == 0) {
                    return value;
                } else if (s > 0) {
                    if (value < -s)
                        value = -s;
                    return value;
                } else {
                    if (value < s)
                        value = -s;
                    return -value;
                }
            }
        }
    }
}

