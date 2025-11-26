/*
 * Copyright (c) 1999, 2021, Oracle and/or its affiliates. All rights reserved.
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

//import java.util.Map;
//import java.util.Vector;
//import java.util.WeakHashMap;

//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.Control;
//import javax.sound.sampled.Line;
//import javax.sound.sampled.LineEvent;
//import javax.sound.sampled.LineListener;
//import javax.sound.sampled.LineUnavailableException;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {
/**
 * AbstractLine
 *
 * @author Kara Kytle
 */
    internal abstract class AbstractLine : ILine {

        protected readonly Line.Info info;
        protected Control[] controls;
        internal AbstractMixer mixer;
        private volatile bool _open;
        private readonly List<object> listeners = new List<object>(); //Vector

        /**
         * Contains event dispatcher per thread group.
         */
        //ConditionalWeakTable (.NET4)
        private static readonly Dictionary<Thread, WeakReference> dispatchers =
                new Dictionary<Thread, WeakReference>();

        /**
         * Constructs a new AbstractLine.
         * @param mixer the mixer with which this line is associated
         * @param controls set of supported controls
         */
        protected AbstractLine(Line.Info info, AbstractMixer mixer, Control[] controls) {

            if (controls == null) {
                controls = new Control[0];
            }

            this.info = info;
            this.mixer = mixer;
            this.controls = controls;
        }

        // LINE METHODS

        public Line.Info getLineInfo() {
            return info;
        }

        public bool isOpen() {
            return _open;
        }

        public void addLineListener(ILineListener listener) {
            lock (listeners) {
                if (!(listeners.Contains(listener))) {
                    listeners.Add(listener);
                }
            }
        }

        /**
         * Removes an audio listener.
         * @param listener listener to remove
         */
        public void removeLineListener(ILineListener listener) {
            listeners.Remove(listener);
        }


        /**
         * Obtains the set of controls supported by the
         * line.  If no controls are supported, returns an
         * array of length 0.
         * @return control set
         */
        public Control[] getControls() {
            Control[] returnedArray = new Control[controls.Length];

            for (int i = 0; i < controls.Length; i++) {
                returnedArray[i] = controls[i];
            }

            return returnedArray;
        }

        public bool isControlSupported(Control.Type controlType) {
            // protect against a NullPointerException
            if (controlType == null) {
                return false;
            }

            for (int i = 0; i < controls.Length; i++) {
                if (controlType == controls[i].getType()) {
                    return true;
                }
            }

            return false;
        }

        public Control getControl(Control.Type controlType) {
            // protect against a NullPointerException
            if (controlType != null) {

                for (int i = 0; i < controls.Length; i++) {
                    if (controlType == controls[i].getType()) {
                        return controls[i];
                    }
                }
            }

            throw new ArgumentException("Unsupported control type: " + controlType);
        }

        // HELPER METHODS

        /**
         * This method sets the open state and generates
         * events if it changes.
         */
        internal void setOpen(bool open) {
            bool _sendEvents = false;
            long position = getLongFramePosition();

            if (this._open != open) {
                this._open = open;
                _sendEvents = true;
            }

            if (_sendEvents) {
                if (open) {
                    sendEvents(new LineEvent(this, LineEvent.Type.OPEN, position));
                } else {
                    sendEvents(new LineEvent(this, LineEvent.Type.CLOSE, position));
                }
            }
        }

        /**
         * Send line events.
         */
        internal void sendEvents(LineEvent evnt) {
            getEventDispatcher().sendAudioEvents(evnt, listeners);
        }

        /**
         * This is an error in the API: getFramePosition
         * should return a long value. At CD quality,
         * the int value wraps around after 13 hours.
         */
        public int getFramePosition() {
            return (int)getLongFramePosition();
        }

        /**
         * Return the frame position in a long value
         * This implementation returns AudioSystem.NOT_SPECIFIED.
         */
        public virtual long getLongFramePosition() {
            return AudioSystem.NOT_SPECIFIED;
        }

        // $$kk: 06.03.99: returns the mixer used in construction.
        // this is a hold-over from when there was a public method like
        // this on line and should be fixed!!
        internal AbstractMixer getMixer() {
            return mixer;
        }

        internal EventDispatcher getEventDispatcher() {
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

        public abstract void open();
        public abstract void close();

        public void Dispose() {
            close();
        }
    }
}
