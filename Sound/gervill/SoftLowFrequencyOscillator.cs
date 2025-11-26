/*
 * Copyright (c) 2007, 2013, Oracle and/or its affiliates. All rights reserved.
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
 * LFO control signal generator.
 *
 * @author Karl Helgason
 */
    public sealed class SoftLowFrequencyOscillator : ISoftProcess {

        private const int max_count = 10;
        private int used_count = 0;
        private readonly double[][] output = new double[max_count][];
        private readonly double[][] delay = new double[max_count][];
        private readonly double[][] delay2 = new double[max_count][];
        private readonly double[][] freq = new double[max_count][];
        private readonly int[] delay_counter = new int[max_count];
        private readonly double[] sin_phase = new double[max_count];
        private readonly double[] sin_stepfreq = new double[max_count];
        private readonly double[] sin_step = new double[max_count];
        private double control_time = 0;
        private double sin_factor = 0;
        private const double PI2 = 2.0 * Math.PI;

        public SoftLowFrequencyOscillator() {
            int i;
            for (i = 0; i < max_count; i++) {
                output[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                delay[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                delay2[i] = new double[1];
            }
            for (i = 0; i < max_count; i++) {
                freq[i] = new double[1];
            }
            // If sin_step is 0 then sin_stepfreq must be -INF
            for (i = 0; i < sin_stepfreq.Length; i++) {
                sin_stepfreq[i] = Double.NegativeInfinity;
            }
        }

        public void reset() {
            for (int i = 0; i < used_count; i++) {
                output[i][0] = 0;
                delay[i][0] = 0;
                delay2[i][0] = 0;
                freq[i][0] = 0;
                delay_counter[i] = 0;
                sin_phase[i] = 0;
                // If sin_step is 0 then sin_stepfreq must be -INF
                sin_stepfreq[i] = Double.NegativeInfinity;
                sin_step[i] = 0;
            }
            used_count = 0;
        }

        public void init(SoftSynthesizer synth) {
            control_time = 1.0 / synth.getControlRate();
            sin_factor = control_time * 2 * Math.PI;
            for (int i = 0; i < used_count; i++) {
                delay_counter[i] = (int)(Math.Pow(2,
                        this.delay[i][0] / 1200.0) / control_time);
                delay_counter[i] += (int)(delay2[i][0] / (control_time * 1000));
            }
            processControlLogic();
        }

        public void processControlLogic() {
            for (int i = 0; i < used_count; i++) {
                if (delay_counter[i] > 0) {
                    delay_counter[i]--;
                    output[i][0] = 0.5;
                } else {
                    double f = freq[i][0];

                    if (sin_stepfreq[i] != f) {
                        sin_stepfreq[i] = f;
                        double fr = 440.0 * Math.Exp(
                                (f - 6900.0) * (Math.Log(2) / 1200.0));
                        sin_step[i] = fr * sin_factor;
                    }
                    /*
                    double fr = 440.0 * Math.Pow(2.0,
                    (freq[i][0] - 6900.0) / 1200.0);
                    sin_phase[i] += fr * sin_factor;
                     */
                    /*
                    sin_phase[i] += sin_step[i];
                    while (sin_phase[i] > PI2)
                    sin_phase[i] -= PI2;
                    output[i][0] = 0.5 + Math.Sin(sin_phase[i]) * 0.5;
                     */
                    double p = sin_phase[i];
                    p += sin_step[i];
                    while (p > PI2)
                        p -= PI2;
                    output[i][0] = 0.5 + Math.Sin(p) * 0.5;
                    sin_phase[i] = p;

                }
            }
        }

        public double[] get(int instance, String name) {
            if (instance >= used_count)
                used_count = instance + 1;
            if (name == null)
                return output[instance];
            if (name.Equals("delay"))
                return delay[instance];
            if (name.Equals("delay2"))
                return delay2[instance];
            if (name.Equals("freq"))
                return freq[instance];
            return null;
        }
    }
}
