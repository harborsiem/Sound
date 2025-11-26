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

//import java.util.Vector;

//import javax.sound.sampled.Control;
//import javax.sound.sampled.Line;
//import javax.sound.sampled.LineUnavailableException;
//import javax.sound.sampled.Mixer;

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {
/**
 * Abstract Mixer.  Implements Mixer (with abstract methods) and specifies
 * some other common methods for use by our implementation.
 *
 * @author Kara Kytle
 */
//$$fb 2002-07-26: let AbstractMixer be an AbstractLine and NOT an AbstractDataLine!
    internal abstract class AbstractMixer : AbstractLine, IMixer {

        //  STATIC VARIABLES
        protected internal const int PCM = 0;
        protected internal const int ULAW = 1;
        protected internal const int ALAW = 2;


        // IMMUTABLE PROPERTIES

        /**
         * Info object describing this mixer.
         */
        private readonly Mixer.Info mixerInfo;

        /**
         * source lines provided by this mixer
         */
        protected Line.Info[] sourceLineInfo;

        /**
         * target lines provided by this mixer
         */
        protected Line.Info[] targetLineInfo;

        /**
         * if any line of this mixer is started
         */
        private bool started = false;

        /**
         * if this mixer had been opened manually with open()
         * If it was, then it won't be closed automatically,
         * only when close() is called manually.
         */
        private bool manuallyOpened = false;

        // STATE VARIABLES

        /**
         * Source lines (ports) currently open.
         */
        private readonly List<ILine> sourceLines = new List<ILine>(); //Vector

        /**
         * Target lines currently open.
         */
        private readonly List<ILine> targetLines = new List<ILine>(); //Vector

        /**
         * Constructs a new AbstractMixer.
         * @param mixerInfo the mixer with which this line is associated
         * @param controls set of supported controls
         */
        protected AbstractMixer(Mixer.Info mixerInfo,
                        Control[] controls,
                    Line.Info[] sourceLineInfo,
                    Line.Info[] targetLineInfo)

        // Line.Info, AbstractMixer, Control[]
            : base(new Line.Info(typeof(IMixer)), null, controls) {

            // setup the line part
            this.mixer = this;
            if (controls == null) {
                controls = Array.Empty<Control>();
            }

            // setup the mixer part
            this.mixerInfo = mixerInfo;
            this.sourceLineInfo = sourceLineInfo;
            this.targetLineInfo = targetLineInfo;
        }

        // MIXER METHODS

        public Mixer.Info getMixerInfo() {
            return mixerInfo;
        }

        public Line.Info[] getSourceLineInfo() {
            Line.Info[] localArray = new Line.Info[sourceLineInfo.Length];
            Array.Copy(sourceLineInfo, 0, localArray, 0, sourceLineInfo.Length);
            return localArray;
        }

        public Line.Info[] getTargetLineInfo() {
            Line.Info[] localArray = new Line.Info[targetLineInfo.Length];
            Array.Copy(targetLineInfo, 0, localArray, 0, targetLineInfo.Length);
            return localArray;
        }

        public Line.Info[] getSourceLineInfo(Line.Info info) {

            int i;
            List<Line.Info> vec = new List<Line.Info>();

            for (i = 0; i < sourceLineInfo.Length; i++) {

                if (info.matches(sourceLineInfo[i])) {
                    vec.Add(sourceLineInfo[i]);
                }
            }

            Line.Info[] returnedArray = new Line.Info[vec.Count];
            for (i = 0; i < returnedArray.Length; i++) {
                returnedArray[i] = vec[i];
            }

            return returnedArray;
        }

        public Line.Info[] getTargetLineInfo(Line.Info info) {

            int i;
            List<Line.Info> vec = new List<Line.Info>();

            for (i = 0; i < targetLineInfo.Length; i++) {

                if (info.matches(targetLineInfo[i])) {
                    vec.Add(targetLineInfo[i]);
                }
            }

            Line.Info[] returnedArray = new Line.Info[vec.Count];
            for (i = 0; i < returnedArray.Length; i++) {
                returnedArray[i] = vec[i];
            }

            return returnedArray;
        }

        public bool isLineSupported(Line.Info info) {

            int i;

            for (i = 0; i < sourceLineInfo.Length; i++) {

                if (info.matches(sourceLineInfo[i])) {
                    return true;
                }
            }

            for (i = 0; i < targetLineInfo.Length; i++) {

                if (info.matches(targetLineInfo[i])) {
                    return true;
                }
            }

            return false;
        }

        public abstract ILine getLine(Line.Info info);

        public abstract int getMaxLines(Line.Info info);

        protected abstract void implOpen();
        protected abstract void implStart();
        protected abstract void implStop();
        protected abstract void implClose();

        public ILine[] getSourceLines() {

            ILine[] localLines = null;

            lock (sourceLines) {

                localLines = new ILine[sourceLines.Count];

                for (int i = 0; i < localLines.Length; i++) {
                    localLines[i] = sourceLines[i];
                }
            }

            return localLines;
        }

        public ILine[] getTargetLines() {

            ILine[] localLines = null;

            lock (targetLines) {

                localLines = new ILine[targetLines.Count];

                for (int i = 0; i < localLines.Length; i++) {
                    localLines[i] = targetLines[i];
                }
            }

            return localLines;
        }

        /**
         * Default implementation always throws an exception.
         */
        public void synchronize(ILine[] lines, bool maintainSync) {
            throw new ArgumentException("Synchronization not supported by this mixer.");
        }

        /**
         * Default implementation always throws an exception.
         */
        public void unsynchronize(ILine[] lines) {
            throw new ArgumentException("Synchronization not supported by this mixer.");
        }

        /**
         * Default implementation always returns false.
         */
        public bool isSynchronizationSupported(ILine[] lines, bool maintainSync) {
            return false;
        }

        // OVERRIDES OF ABSTRACT DATA LINE METHODS

        /**
         * This implementation tries to open the mixer with its current format and buffer size settings.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public sealed override void open() {
            open(true);
        }

        /**
         * This implementation tries to open the mixer with its current format and buffer size settings.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void open(bool manual) {
            if (!isOpen()) {
                implOpen();
                // if the mixer is not currently open, set open to true and send event
                setOpen(true);
                if (manual) {
                    manuallyOpened = true;
                }
            }
        }

        // METHOD FOR INTERNAL IMPLEMENTATION USE

        /**
         * The default implementation of this method just determines whether
         * this line is a source or target line, calls open(no-arg) on the
         * mixer, and adds the line to the appropriate vector.
         * The mixer may be opened at a format different than the line's
         * format if it is a DataLine.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void open(ILine line) {
            // $$kk: 06.11.99: ignore ourselves for now
            if (this.Equals(line)) {
                return;
            }

            // source line?
            if (isSourceLine(line.getLineInfo())) {
                if (!sourceLines.Contains(line)) {
                    // call the no-arg open method for the mixer; it should open at its
                    // default format if it is not open yet
                    open(false);

                    // we opened successfully! add the line to the list
                    sourceLines.Add(line);
                }
            } else {
                // target line?
                if (isTargetLine(line.getLineInfo())) {
                    if (!targetLines.Contains(line)) {
                        // call the no-arg open method for the mixer; it should open at its
                        // default format if it is not open yet
                        open(false);

                        // we opened successfully!  add the line to the list
                        targetLines.Add(line);
                    }
                } else {
                    if (Printer.err) Printer.Err("Unknown line received for AbstractMixer.open(Line): " + line);
                }
            }
        }

        /**
         * Removes this line from the list of open source lines and
         * open target lines, if it exists in either.
         * If the list is now empty, closes the mixer.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void close(ILine line) {
            // $$kk: 06.11.99: ignore ourselves for now
            if (this.Equals(line)) {
                return;
            }

            sourceLines.Remove(line);
            targetLines.Remove(line);

            if (sourceLines.Count == 0 && targetLines.Count == 0 && !manuallyOpened) {
                close();
            }
        }

        /**
         * Close all lines and then close this mixer.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public sealed override void close() {
            if (isOpen()) {
                // close all source lines
                ILine[] localLines = getSourceLines();
                for (int i = 0; i < localLines.Length; i++) {
                    localLines[i].close();
                }

                // close all target lines
                localLines = getTargetLines();
                for (int i = 0; i < localLines.Length; i++) {
                    localLines[i].close();
                }

                implClose();

                // set the open state to false and send events
                setOpen(false);
            }
            manuallyOpened = false;
        }

        /**
         * Starts the mixer.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void start(ILine line) {
            // $$kk: 06.11.99: ignore ourselves for now
            if (this.Equals(line)) {
                return;
            }

            // we just start the mixer regardless of anything else here.
            if (!started) {
                implStart();
                started = true;
            }
        }

        /**
         * Stops the mixer if this was the last running line.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void stop(ILine line) {
            // $$kk: 06.11.99: ignore ourselves for now
            if (this.Equals(line)) {
                return;
            }

            List<ILine> localSourceLines = new List<ILine>(sourceLines); //Vector
            for (int i = 0; i < localSourceLines.Count; i++) {

                // if any other open line is running, return

                // this covers clips and source data lines
                if (localSourceLines[i] is AbstractDataLine) {
                    AbstractDataLine sourceLine = (AbstractDataLine)localSourceLines[i];
                    if (sourceLine.isStartedRunning() && (!sourceLine.Equals(line))) {
                        return;
                    }
                }
            }

            List<ILine> localTargetLines = new List<ILine>(targetLines); //Vector
            for (int i = 0; i < localTargetLines.Count; i++) {

                // if any other open line is running, return
                // this covers target data lines
                if (localTargetLines[i] is AbstractDataLine) {
                    AbstractDataLine targetLine = (AbstractDataLine)localTargetLines[i];
                    if (targetLine.isStartedRunning() && (!targetLine.Equals(line))) {
                        return;
                    }
                }
            }

            // otherwise, stop
            started = false;
            implStop();
        }

        /**
         * Determines whether this is a source line for this mixer.
         * Right now this just checks whether it's supported, but should
         * check whether it actually belongs to this mixer....
         */
        internal bool isSourceLine(Line.Info info) {

            for (int i = 0; i < sourceLineInfo.Length; i++) {
                if (info.matches(sourceLineInfo[i])) {
                    return true;
                }
            }

            return false;
        }

        /**
         * Determines whether this is a target line for this mixer.
         * Right now this just checks whether it's supported, but should
         * check whether it actually belongs to this mixer....
         */
        internal bool isTargetLine(Line.Info info) {

            for (int i = 0; i < targetLineInfo.Length; i++) {
                if (info.matches(targetLineInfo[i])) {
                    return true;
                }
            }

            return false;
        }

        /**
         * Returns the first complete Line.Info object it finds that
         * matches the one specified, or null if no matching Line.Info
         * object is found.
         */
        internal Line.Info getLineInfo(Line.Info info) {
            if (info == null) {
                return null;
            }
            // $$kk: 05.31.99: need to change this so that
            // the format and buffer size get set in the
            // returned info object for data lines??
            for (int i = 0; i < sourceLineInfo.Length; i++) {
                if (info.matches(sourceLineInfo[i])) {
                    return sourceLineInfo[i];
                }
            }

            for (int i = 0; i < targetLineInfo.Length; i++) {
                if (info.matches(targetLineInfo[i])) {
                    return targetLineInfo[i];
                }
            }

            return null;
        }
    }
}
