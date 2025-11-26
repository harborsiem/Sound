/*
 * Copyright (c) 2007, Oracle and/or its affiliates. All rights reserved.
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
 * ModelAbstractChannelMixer is ready for use class to implement
 * ModelChannelMixer interface.
 *
 * @author Karl Helgason
 */
    public abstract class ModelAbstractChannelMixer : IModelChannelMixer {

        public abstract bool process(float[][] buffer, int offset, int len);

        public abstract void stop();

        public virtual void allNotesOff() {
        }

        public virtual void allSoundOff() {
        }

        public virtual void controlChange(int controller, int value) {
        }

        public virtual int getChannelPressure() {
            return 0;
        }

        public virtual int getController(int controller) {
            return 0;
        }

        public virtual bool getMono() {
            return false;
        }

        public virtual bool getMute() {
            return false;
        }

        public virtual bool getOmni() {
            return false;
        }

        public virtual int getPitchBend() {
            return 0;
        }

        public virtual int getPolyPressure(int noteNumber) {
            return 0;
        }

        public virtual int getProgram() {
            return 0;
        }

        public virtual bool getSolo() {
            return false;
        }

        public virtual bool localControl(bool on) {
            return false;
        }

        public virtual void noteOff(int noteNumber) {
        }

        public virtual void noteOff(int noteNumber, int velocity) {
        }

        public virtual void noteOn(int noteNumber, int velocity) {
        }

        public virtual void programChange(int program) {
        }

        public virtual void programChange(int bank, int program) {
        }

        public virtual void resetAllControllers() {
        }

        public virtual void setChannelPressure(int pressure) {
        }

        public virtual void setMono(bool on) {
        }

        public virtual void setMute(bool mute) {
        }

        public virtual void setOmni(bool on) {
        }

        public virtual void setPitchBend(int bend) {
        }

        public virtual void setPolyPressure(int noteNumber, int pressure) {
        }

        public virtual void setSolo(bool soloState) {
        }
    }
}
