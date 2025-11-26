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
 * This class stores the identity of source and destinations in connection
 * blocks, see ModelConnectionBlock.
 *
 * @author Karl Helgason
 */
    public sealed class ModelIdentifier {

        /*
         *  Object    Variable
         *  ------    --------
         *
         *  // INPUT parameters
         *  noteon    keynumber                7 bit midi value
         *            velocity                 7 bit midi value
         *            on                       1 or 0
         *
         *  midi      pitch                    14 bit midi value
         *            channel_pressure         7 bit midi value
         *            poly_pressure            7 bit midi value
         *
         *  midi_cc   0 (midi control #0       7 bit midi value
         *            1 (midi control #1       7 bit midi value
         *            ...
         *            127 (midi control #127   7 bit midi value
         *
         *  midi_rpn  0 (midi rpn control #0)  14 bit midi value
         *            1 (midi rpn control #1)  14 bit midi value
         *            ....
         *
         *  // DAHDSR envelope generator
         *  eg        (null)
         *            delay                    timecent
         *            attack                   timecent
         *            hold                     timecent
         *            decay                    timecent
         *            sustain                  0.1 %
         *            release                  timecent
         *
         *  // Low frequency oscillirator (sine wave)
         *  lfo       (null)
         *            delay                    timcent
         *            freq                     cent
         *
         *  // Resonance LowPass Filter 6dB slope
         *  filter    (null) (output/input)
         *            freq                     cent
         *            q                        cB
         *
         *  // The oscillator with preloaded wavetable data
         *  osc       (null)
         *            pitch                    cent
         *
         *  // Output mixer pins
         *  mixer     gain                     cB
         *            pan                      0.1 %
         *            reverb                   0.1 %
         *            chorus                   0.1 %
         *
         */
        private String object0 = null;
        private String variable = null;
        private int instance = 0;

        public ModelIdentifier(String object0) {
            this.object0 = object0;
        }

        public ModelIdentifier(String object0, int instance) {
            this.object0 = object0;
            this.instance = instance;
        }

        public ModelIdentifier(String object0, String variable) {
            this.object0 = object0;
            this.variable = variable;

        }

        public ModelIdentifier(String object0, String variable, int instance) {
            this.object0 = object0;
            this.variable = variable;
            this.instance = instance;

        }

        public int getInstance() {
            return instance;
        }

        public void setInstance(int instance) {
            this.instance = instance;
        }

        public String getObject() {
            return object0;
        }

        public void setObject(String object0) {
            this.object0 = object0;
        }

        public String getVariable() {
            return variable;
        }

        public void setVariable(String variable) {
            this.variable = variable;
        }

        public override int GetHashCode() {
            int hashcode = instance;
            if (object0 != null) hashcode |= object0.GetHashCode();
            if (variable != null) hashcode |= variable.GetHashCode();
            return hashcode;
        }

        public override bool Equals(Object obj) {
            if (!(obj is ModelIdentifier))
                return false;

            ModelIdentifier mobj = (ModelIdentifier)obj;
            if ((object0 == null) != (mobj.object0 == null))
                return false;
            if ((variable == null) != (mobj.variable == null))
                return false;
            if (mobj.getInstance() != getInstance())
                return false;
            if (!(object0 == null || object0.Equals(mobj.object0)))
                return false;
            if (!(variable == null || variable.Equals(mobj.variable)))
                return false;
            return true;
        }

        public override String ToString() {
            if (variable == null) {
                return object0 + "[" + instance + "]";
            } else {
                return object0 + "[" + instance + "]" + "." + variable;
            }
        }
    }
}
