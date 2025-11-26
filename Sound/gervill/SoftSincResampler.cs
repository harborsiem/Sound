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
 * Hann windowed sinc interpolation resampler with anti-alias filtering.
 *
 * Using 30 points for the interpolation.
 *
 * @author Karl Helgason
 */
    public sealed class SoftSincResampler : SoftAbstractResampler {

        internal float[][][] sinc_table;
        internal int sinc_scale_size = 100;
        internal int sinc_table_fsize = 800;
        internal const int sinc_table_size = 30;
        internal int sinc_table_center = sinc_table_size / 2;

        public SoftSincResampler()
            : base() {
            sinc_table = new float[sinc_scale_size][][];
            for (int i0 = 0; i0 < sinc_table.Length; i0++) {
                sinc_table[i0] = new float[sinc_table_fsize][];
            }
            for (int s = 0; s < sinc_scale_size; s++) {
                float scale = (float)(1.0 / (1.0 + Math.Pow(s, 1.1) / 10.0));
                for (int i = 0; i < sinc_table_fsize; i++) {
                    sinc_table[s][i] = sincTable(sinc_table_size,
                            -i / ((float)sinc_table_fsize), scale);
                }
            }
        }

        // Normalized sinc function
        public static double sinc(double x) {
            return (x == 0.0) ? 1.0 : Math.Sin(Math.PI * x) / (Math.PI * x);
        }

        // Generate hann window suitable for windowing sinc
        public static float[] wHanning(int size, float offset) {
            float[] window_table = new float[size];
            for (int k = 0; k < size; k++) {
                window_table[k] = (float)(-0.5
                        * Math.Cos(2.0 * Math.PI * (double)(k + offset)
                            / (double)size) + 0.5);
            }
            return window_table;
        }

        // Generate sinc table
        public static float[] sincTable(int size, float offset, float scale) {
            int center = size / 2;
            float[] w = wHanning(size, offset);
            for (int k = 0; k < size; k++)
                w[k] *= (float)(sinc((-center + k + offset) * scale) * scale);
            return w;
        }

        public override int getPadding() // must be at least half of sinc_table_size
        {
            return sinc_table_size / 2 + 2;
        }

        public override void interpolate(float[] input, float[] in_offset, float in_end,
                                         float[] startpitch, float pitchstep, float[] output, int[] out_offset,
                                         int out_end) {
            float pitch = startpitch[0];
            float ix = in_offset[0];
            int ox = out_offset[0];
            float ix_end = in_end;
            int ox_end = out_end;
            int max_p = sinc_scale_size - 1;
            if (pitchstep == 0) {

                int p = (int)((pitch - 1) * 10.0f);
                if (p < 0)
                    p = 0;
                else if (p > max_p)
                    p = max_p;
                float[][] sinc_table_f = this.sinc_table[p];
                while (ix < ix_end && ox < ox_end) {
                    int iix = (int)ix;
                    float[] sinc_table =
                            sinc_table_f[(int)((ix - iix) * sinc_table_fsize)];
                    int xx = iix - sinc_table_center;
                    float y = 0;
                    for (int i = 0; i < sinc_table_size; i++, xx++)
                        y += input[xx] * sinc_table[i];
                    output[ox++] = y;
                    ix += pitch;
                }
            } else {
                while (ix < ix_end && ox < ox_end) {
                    int iix = (int)ix;
                    int p = (int)((pitch - 1) * 10.0f);
                    if (p < 0)
                        p = 0;
                    else if (p > max_p)
                        p = max_p;
                    float[][] sinc_table_f = this.sinc_table[p];

                    float[] sinc_table =
                            sinc_table_f[(int)((ix - iix) * sinc_table_fsize)];
                    int xx = iix - sinc_table_center;
                    float y = 0;
                    for (int i = 0; i < sinc_table_size; i++, xx++)
                        y += input[xx] * sinc_table[i];
                    output[ox++] = y;

                    ix += pitch;
                    pitch += pitchstep;
                }
            }
            in_offset[0] = ix;
            out_offset[0] = ox;
            startpitch[0] = pitch;
        }
    }
}
