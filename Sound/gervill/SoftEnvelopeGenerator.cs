/*
 * Copyright (c) 2007, 2014, Oracle and/or its affiliates. All rights reserved.
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

using System;
using System.Collections.Generic;
using System.Text;

namespace SystemX.Media.Sound {

/**
 * AHDSR control signal envelope generator.
 *
 * @author Karl Helgason
 */
    public sealed class SoftEnvelopeGenerator : ISoftProcess {

        public const int EG_OFF = 0;
        public const int EG_DELAY = 1;
        public const int EG_ATTACK = 2;
        public const int EG_HOLD = 3;
        public const int EG_DECAY = 4;
        public const int EG_SUSTAIN = 5;
        public const int EG_RELEASE = 6;
        public const int EG_SHUTDOWN = 7;
        public const int EG_END = 8;
        internal const int max_count = 10; //a@
        internal int used_count = 0;
        private readonly int[] stage = new int[max_count];
        private readonly int[] stage_ix = new int[max_count];
        private readonly double[] stage_v = new double[max_count];
        private readonly int[] stage_count = new int[max_count];
        private readonly double[][] on = new double[max_count][];
        private readonly double[][] active = new double[max_count][];
        private readonly double[][] output = new double[max_count][];
        private readonly double[][] delay = new double[max_count][];
        private readonly double[][] attack = new double[max_count][];
        private readonly double[][] hold = new double[max_count][];
        private readonly double[][] decay = new double[max_count][];
        private readonly double[][] sustain = new double[max_count][];
        private readonly double[][] release = new double[max_count][];
        private readonly double[][] shutdown = new double[max_count][];
        private readonly double[][] release2 = new double[max_count][];
        private readonly double[][] attack2 = new double[max_count][];
        private readonly double[][] decay2 = new double[max_count][];
        private double control_time = 0;

        public SoftEnvelopeGenerator() {
            int i;
            for (i = 0; i < max_count; i++) {
                on[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                active[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                output[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                delay[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                attack[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                hold[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                decay[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                sustain[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                release[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                shutdown[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                release2[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                attack2[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                decay2[i] = new double[1];
            }
        }

        public void reset() {
            for (int i = 0; i < used_count; i++) {
                stage[i] = 0;
                on[i][0] = 0;
                output[i][0] = 0;
                delay[i][0] = 0;
                attack[i][0] = 0;
                hold[i][0] = 0;
                decay[i][0] = 0;
                sustain[i][0] = 0;
                release[i][0] = 0;
                shutdown[i][0] = 0;
                attack2[i][0] = 0;
                decay2[i][0] = 0;
                release2[i][0] = 0;
            }
            used_count = 0;
        }

        public void init(SoftSynthesizer synth) {
            control_time = 1.0 / synth.getControlRate();
            processControlLogic();
        }

        public double[] get(int instance, String name) {
            if (instance >= used_count)
                used_count = instance + 1;
            if (name == null)
                return output[instance];
            if (name.Equals("on"))
                return on[instance];
            if (name.Equals("active"))
                return active[instance];
            if (name.Equals("delay"))
                return delay[instance];
            if (name.Equals("attack"))
                return attack[instance];
            if (name.Equals("hold"))
                return hold[instance];
            if (name.Equals("decay"))
                return decay[instance];
            if (name.Equals("sustain"))
                return sustain[instance];
            if (name.Equals("release"))
                return release[instance];
            if (name.Equals("shutdown"))
                return shutdown[instance];
            if (name.Equals("attack2"))
                return attack2[instance];
            if (name.Equals("decay2"))
                return decay2[instance];
            if (name.Equals("release2"))
                return release2[instance];

            return null;
        }

        public void processControlLogic() {
            for (int i = 0; i < used_count; i++) {

                if (stage[i] == EG_END)
                    continue;

                if ((stage[i] > EG_OFF) && (stage[i] < EG_RELEASE)) {
                    if (on[i][0] < 0.5) {
                        if (on[i][0] < -0.5) {
                            stage_count[i] = (int)(Math.Pow(2,
                                    this.shutdown[i][0] / 1200.0) / control_time);
                            if (stage_count[i] < 0)
                                stage_count[i] = 0;
                            stage_v[i] = output[i][0];
                            stage_ix[i] = 0;
                            stage[i] = EG_SHUTDOWN;
                        } else {
                            if ((release2[i][0] < 0.000001) && release[i][0] < 0
                                    && Double.IsInfinity(release[i][0])) {
                                output[i][0] = 0;
                                active[i][0] = 0;
                                stage[i] = EG_END;
                                continue;
                            }

                            stage_count[i] = (int)(Math.Pow(2,
                                    this.release[i][0] / 1200.0) / control_time);
                            stage_count[i]
                                    += (int)(this.release2[i][0] / (control_time * 1000));
                            if (stage_count[i] < 0)
                                stage_count[i] = 0;
                            // stage_v[i] = output[i][0];
                            stage_ix[i] = 0;

                            double m = 1 - output[i][0];
                            stage_ix[i] = (int)(stage_count[i] * m);

                            stage[i] = EG_RELEASE;
                        }
                    }
                }

                switch (stage[i]) {
                    case EG_OFF:
                        active[i][0] = 1;
                        if (on[i][0] < 0.5)
                            break;
                        stage[i] = EG_DELAY;
                        stage_ix[i] = (int)(Math.Pow(2,
                                this.delay[i][0] / 1200.0) / control_time);
                        if (stage_ix[i] < 0)
                            stage_ix[i] = 0;

                        goto case EG_DELAY; //a@
                                            // Fallthrough
                    case EG_DELAY:
                        if (stage_ix[i] == 0) {
                            double attack = this.attack[i][0];
                            double attack2 = this.attack2[i][0];

                            if (attack2 < 0.000001
                                    && (attack < 0 && Double.IsInfinity(attack))) {
                                output[i][0] = 1;
                                stage[i] = EG_HOLD;
                                stage_count[i] = (int)(Math.Pow(2,
                                        this.hold[i][0] / 1200.0) / control_time);
                                stage_ix[i] = 0;
                            } else {
                                stage[i] = EG_ATTACK;
                                stage_count[i] = (int)(Math.Pow(2,
                                        attack / 1200.0) / control_time);
                                stage_count[i] += (int)(attack2 / (control_time * 1000));
                                if (stage_count[i] < 0)
                                    stage_count[i] = 0;
                                stage_ix[i] = 0;
                            }
                        } else
                            stage_ix[i]--;
                        break;
                    case EG_ATTACK:
                        stage_ix[i]++;
                        if (stage_ix[i] >= stage_count[i]) {
                            output[i][0] = 1;
                            stage[i] = EG_HOLD;
                        } else {
                            // CONVEX attack
                            double a = ((double)stage_ix[i]) / ((double)stage_count[i]);
                            a = 1 + ((40.0 / 96.0) / Math.Log(10)) * Math.Log(a);
                            if (a < 0)
                                a = 0;
                            else if (a > 1)
                                a = 1;
                            output[i][0] = a;
                        }
                        break;
                    case EG_HOLD:
                        stage_ix[i]++;
                        if (stage_ix[i] >= stage_count[i]) {
                            stage[i] = EG_DECAY;
                            stage_count[i] = (int)(Math.Pow(2,
                                    this.decay[i][0] / 1200.0) / control_time);
                            stage_count[i] += (int)(this.decay2[i][0] / (control_time * 1000));
                            if (stage_count[i] < 0)
                                stage_count[i] = 0;
                            stage_ix[i] = 0;
                        }
                        break;
                    case EG_DECAY:
                        stage_ix[i]++;
                        double sustain = this.sustain[i][0] * (1.0 / 1000.0);
                        if (stage_ix[i] >= stage_count[i]) {
                            output[i][0] = sustain;
                            stage[i] = EG_SUSTAIN;
                            if (sustain < 0.001) {
                                output[i][0] = 0;
                                active[i][0] = 0;
                                stage[i] = EG_END;
                            }
                        } else {
                            double m = ((double)stage_ix[i]) / ((double)stage_count[i]);
                            output[i][0] = (1 - m) + sustain * m;
                        }
                        break;
                    case EG_SUSTAIN:
                        break;
                    case EG_RELEASE:
                        stage_ix[i]++;
                        if (stage_ix[i] >= stage_count[i]) {
                            output[i][0] = 0;
                            active[i][0] = 0;
                            stage[i] = EG_END;
                        } else {
                            double m = ((double)stage_ix[i]) / ((double)stage_count[i]);
                            output[i][0] = (1 - m); // *stage_v[i];

                            if (on[i][0] < -0.5) {
                                stage_count[i] = (int)(Math.Pow(2,
                                        this.shutdown[i][0] / 1200.0) / control_time);
                                if (stage_count[i] < 0)
                                    stage_count[i] = 0;
                                stage_v[i] = output[i][0];
                                stage_ix[i] = 0;
                                stage[i] = EG_SHUTDOWN;
                            }

                            // re-damping
                            if (on[i][0] > 0.5) {
                                sustain = this.sustain[i][0] * (1.0 / 1000.0);
                                if (output[i][0] > sustain) {
                                    stage[i] = EG_DECAY;
                                    stage_count[i] = (int)(Math.Pow(2,
                                            this.decay[i][0] / 1200.0) / control_time);
                                    stage_count[i] +=
                                            (int)(this.decay2[i][0] / (control_time * 1000));
                                    if (stage_count[i] < 0)
                                        stage_count[i] = 0;
                                    m = (output[i][0] - 1) / (sustain - 1);
                                    stage_ix[i] = (int)(stage_count[i] * m);
                                }
                            }

                        }
                        break;
                    case EG_SHUTDOWN:
                        stage_ix[i]++;
                        if (stage_ix[i] >= stage_count[i]) {
                            output[i][0] = 0;
                            active[i][0] = 0;
                            stage[i] = EG_END;
                        } else {
                            double m = ((double)stage_ix[i]) / ((double)stage_count[i]);
                            output[i][0] = (1 - m) * stage_v[i];
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
