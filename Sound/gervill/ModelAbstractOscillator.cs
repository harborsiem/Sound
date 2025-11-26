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

//import java.io.IOException;

//import javax.sound.midi.Instrument;
//import javax.sound.midi.MidiChannel;
//import javax.sound.midi.Patch;
//import javax.sound.midi.Soundbank;
//import javax.sound.midi.SoundbankResource;
//import javax.sound.midi.VoiceStatus;

using System;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Reflection;
using System.Globalization;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {

/**
 * A abstract class used to simplify creating custom ModelOscillator.
 *
 * @author Karl Helgason
 */
    public abstract class ModelAbstractOscillator
            : IModelOscillator, IModelOscillatorStream, ISoundbank {

        protected float pitch = 6000;
        protected float samplerate;
        protected IMidiChannel channel;
        protected VoiceStatus voice;
        protected int noteNumber;
        protected int velocity;
        protected bool on = false;

        public virtual void init() {
        }

        public virtual void close() {
        }

        public void noteOff(int velocity) {
            on = false;
        }

        public void noteOn(IMidiChannel channel, VoiceStatus voice, int noteNumber,
                           int velocity) {
            this.channel = channel;
            this.voice = voice;
            this.noteNumber = noteNumber;
            this.velocity = velocity;
            on = true;
        }

        public virtual int read(float[][] buffer, int offset, int len) {
            return -1;
        }

        public IMidiChannel getChannel() {
            return channel;
        }

        public VoiceStatus getVoice() {
            return voice;
        }

        public int getNoteNumber() {
            return noteNumber;
        }

        public int getVelocity() {
            return velocity;
        }

        public bool isOn() {
            return on;
        }

        public void setPitch(float pitch) {
            this.pitch = pitch;
        }

        public float getPitch() {
            return pitch;
        }

        public void setSampleRate(float samplerate) {
            this.samplerate = samplerate;
        }

        public float getSampleRate() {
            return samplerate;
        }

        public virtual float getAttenuation() {
            return 0;
        }

        public virtual int getChannels() {
            return 1;
        }

        public String getName() {
            return GetType().FullName;
        }

        public virtual Patch getPatch() {
            return new Patch(0, 0);
        }

        public IModelOscillatorStream open(float samplerate) {
            ModelAbstractOscillator oscs;
            try {
                oscs = (ModelAbstractOscillator)this.GetType().InvokeMember(null, BindingFlags.CreateInstance, null, null, null, CultureInfo.InvariantCulture); //.newInstance(); //@
            }
            catch (TargetInvocationException e) {
                throw new ArgumentException("", e);
            }
            catch (SecurityException e) {
                throw new ArgumentException("", e);
            }
            oscs.setSampleRate(samplerate);
            oscs.init();
            return oscs;
        }

        public ModelPerformer getPerformer() {
            // Create performer for my custom oscillirator
            ModelPerformer performer = new ModelPerformer();
            performer.getOscillators().Add(this);
            return performer;

        }

        public ModelInstrument getInstrument() {
            // Create Instrument object around my performer
            SimpleInstrument ins = new SimpleInstrument();
            ins.setName(getName());
            ins.Add(getPerformer());
            ins.setPatch(getPatch());
            return ins;

        }

        public ISoundbank getSoundBank() {
            // Create Soundbank object around the instrument
            SimpleSoundbank sbk = new SimpleSoundbank();
            sbk.addInstrument(getInstrument());
            return sbk;
        }

        public String getDescription() {
            return getName();
        }

        public Instrument getInstrument(Patch patch) {
            Instrument ins = getInstrument();
            Patch p = ins.getPatch();
            if (p.getBank() != patch.getBank())
                return null;
            if (p.getProgram() != patch.getProgram())
                return null;
            if (p is ModelPatch && patch is ModelPatch) {
                if (((ModelPatch)p).isPercussion()
                        != ((ModelPatch)patch).isPercussion()) {
                    return null;
                }
            }
            return ins;
        }

        public virtual Instrument[] getInstruments() {
            return new Instrument[] { getInstrument() };
        }

        public virtual SoundbankResource[] getResources() {
            return new SoundbankResource[0];
        }

        public virtual String getVendor() {
            return null;
        }

        public virtual String getVersion() {
            return null;
        }
    }
}
