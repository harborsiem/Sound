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
 * A standard transformer used in connection blocks.
 * It expects input values to be between 0 and 1.
 *
 * The result of the transform is
 *   between 0 and 1 if polarity = unipolar and
 *   between -1 and 1 if polarity = bipolar.
 *
 * These constraints only applies to Concave, Convex and Switch transforms.
 *
 * @author Karl Helgason
 */
    public sealed class ModelStandardTransform : IModelTransform {

        public const bool DIRECTION_MIN2MAX = false;
        public const bool DIRECTION_MAX2MIN = true;
        public const bool POLARITY_UNIPOLAR = false;
        public const bool POLARITY_BIPOLAR = true;
        public const int TRANSFORM_LINEAR = 0;
        // concave: output = (20*log10(127^2/value^2)) / 96
        public const int TRANSFORM_CONCAVE = 1;
        // convex: same as concave except that start and end point are reversed.
        public const int TRANSFORM_CONVEX = 2;
        // switch: if value > avg(max,min) then max else min
        public const int TRANSFORM_SWITCH = 3;
        public const int TRANSFORM_ABSOLUTE = 4;
        private bool direction = DIRECTION_MIN2MAX;
        private bool polarity = POLARITY_UNIPOLAR;
        private int _transform = TRANSFORM_LINEAR;

        public ModelStandardTransform() {
        }

        public ModelStandardTransform(bool direction) {
            this.direction = direction;
        }

        public ModelStandardTransform(bool direction, bool polarity) {
            this.direction = direction;
            this.polarity = polarity;
        }

        public ModelStandardTransform(bool direction, bool polarity,
                int transform) {
            this.direction = direction;
            this.polarity = polarity;
            this._transform = transform;
        }

        public double transform(double value) {
            double s;
            double a;
            if (direction == DIRECTION_MAX2MIN)
                value = 1.0 - value;
            if (polarity == POLARITY_BIPOLAR)
                value = value * 2.0 - 1.0;
            switch (_transform) {
                case TRANSFORM_CONCAVE:
                    s = Math.Sign(value);
                    a = Math.Abs(value);
                    a = -((5.0 / 12.0) / Math.Log(10)) * Math.Log(1.0 - a);
                    if (a < 0)
                        a = 0;
                    else if (a > 1)
                        a = 1;
                    return s * a;
                case TRANSFORM_CONVEX:
                    s = Math.Sign(value);
                    a = Math.Abs(value);
                    a = 1.0 + ((5.0 / 12.0) / Math.Log(10)) * Math.Log(a);
                    if (a < 0)
                        a = 0;
                    else if (a > 1)
                        a = 1;
                    return s * a;
                case TRANSFORM_SWITCH:
                    if (polarity == POLARITY_BIPOLAR)
                        return (value > 0) ? 1 : -1;
                    else
                        return (value > 0.5) ? 1 : 0;
                case TRANSFORM_ABSOLUTE:
                    return Math.Abs(value);
                default:
                    break;
            }

            return value;
        }

        public bool getDirection() {
            return direction;
        }

        public void setDirection(bool direction) {
            this.direction = direction;
        }

        public bool getPolarity() {
            return polarity;
        }

        public void setPolarity(bool polarity) {
            this.polarity = polarity;
        }

        public int getTransform() {
            return _transform;
        }

        public void setTransform(int transform) {
            this._transform = transform;
        }
    }
}
