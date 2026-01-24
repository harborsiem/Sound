/*
 * Copyright (c) 1998, 2021, Oracle and/or its affiliates. All rights reserved.
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
//import java.util.List;

//import javax.sound.midi.ControllerEventListener;
//import javax.sound.midi.MetaEventListener;
//import javax.sound.midi.MetaMessage;
//import javax.sound.midi.ShortMessage;
//import javax.sound.sampled.LineEvent;
//import javax.sound.sampled.LineListener;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using SystemX.Addon;
using SystemX.Sound.Sampled;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {

/**
 * EventDispatcher.  Used by various classes in the Java Sound implementation
 * to send events.
 *
 * @author David Rivas
 * @author Kara Kytle
 * @author Florian Bomers
 */
    internal sealed class EventDispatcher : IRunnable {

        /**
         * time of inactivity until the auto closing clips
         * are closed.
         */
        private const int AUTO_CLOSE_TIME = 5000;

        /**
         * List of events.
         */
        private readonly List<EventInfo> eventQueue = new List<EventInfo>();

        /**
         * Thread object for this EventDispatcher instance.
         */
        private Thread thread = null;

        /*
         * support for auto-closing Clips
         */
        private readonly List<ClipInfo> autoClosingClips = new List<ClipInfo>();

        /*
         * support for monitoring data lines
         */
        private readonly List<ILineMonitor> lineMonitors = new List<ILineMonitor>();

        /**
         * Approximate interval between calls to LineMonitor.checkLine
         */
        internal const int LINE_MONITOR_TIME = 400;

        /**
         * This start() method starts an event thread if one is not already active.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal void start() {
            if (thread == null) {
                thread = JSSecurityManager.createThread(this.run,
                                   "Java Sound Event Dispatcher",   // name
                                   true,  // daemon
                                   ThreadPriority.Normal, //-1,    // priority
                                   true); // doStart
            }
        }

        /**
         * Invoked when there is at least one event in the queue.
         * Implement this as a callback to process one event.
         */
        internal void processEvent(EventInfo eventInfo) {
            int count = eventInfo.getListenerCount();

            // process an LineEvent
            if (eventInfo.getEvent() is LineEvent) {
                LineEvent evnt = (LineEvent)eventInfo.getEvent();
                for (int i = 0; i < count; i++) {
                    try {
                        ((ILineListener)eventInfo.getListener(i)).update(evnt);
                    } catch (Exception t) {
                        if (Printer.err) printStackTrace(t);
                    }
                }
                return;
            }

            // process a MetaMessage
            if (eventInfo.getEvent() is MetaMessage) {
                MetaMessage evnt = (MetaMessage)eventInfo.getEvent();
                for (int i = 0; i < count; i++) {
                    try {
                        ((IMetaEventListener)eventInfo.getListener(i)).meta(evnt);
                    } catch (Exception t) {
                        if (Printer.err) printStackTrace(t);
                    }
                }
                return;
            }

            // process a Controller or Mode Event
            if (eventInfo.getEvent() is ShortMessage) {
                ShortMessage evnt = (ShortMessage)eventInfo.getEvent();
                int status = evnt.getStatus();

                // Controller and Mode events have status byte 0xBc, where
                // c is the channel they are sent on.
                if ((status & 0xF0) == 0xB0) {
                    for (int i = 0; i < count; i++) {
                        try {
                            ((IControllerEventListener)eventInfo.getListener(i)).controlChange(evnt);
                        } catch (Exception t) {
                            if (Printer.err) printStackTrace(t);
                        }
                    }
                }
                return;
            }

            Printer.Err("Unknown event type: " + eventInfo.getEvent());
        }

        /**
         * Wait until there is something in the event queue to process.  Then
         * dispatch the event to the listeners.The entire method does not
         * need to be synchronized since this includes taking the event out
         * from the queue and processing the event. We only need to provide
         * exclusive access over the code where an event is removed from the
         *queue.
         */
        internal void dispatchEvents() {

            EventInfo eventInfo = null;

            lock (this) {

                // Wait till there is an event in the event queue.
                try {

                    if (eventQueue.Count == 0) {
                        if (autoClosingClips.Count > 0 || lineMonitors.Count > 0) {
                            int waitTime = AUTO_CLOSE_TIME;
                            if (lineMonitors.Count > 0) {
                                waitTime = LINE_MONITOR_TIME;
                            }
                            Monitor.Wait(this, waitTime);
                        } else {
                            Monitor.Wait(this);
                        }
                    }
                } catch (ThreadInterruptedException) {
                }
                if (eventQueue.Count > 0) {
                    // Remove the event from the queue and dispatch it to the listeners.
                    eventInfo = eventQueue[0];
                    eventQueue.RemoveAt(0);
                }

            } // end of synchronized
            if (eventInfo != null) {
                processEvent(eventInfo);
            } else {
                if (autoClosingClips.Count > 0) {
                    closeAutoClosingClips();
                }
                if (lineMonitors.Count > 0) {
                    monitorLines();
                }
            }
        }

        /**
         * Queue the given event in the event queue.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        private void postEvent(EventInfo eventInfo) {
            eventQueue.Add(eventInfo);
            Monitor.PulseAll(this); // notifyAll();
        }

        /**
         * A loop to dispatch events.
         */
        public void run() {

            while (true) {
                try {
                    dispatchEvents();
                } catch (Exception t) {
                    if (Printer.err) printStackTrace(t);
                }
            }
        }

        /**
         * Send audio and MIDI events.
         */
        internal void sendAudioEvents(Object evnt, IList<Object> listeners) {
            if ((listeners == null)
                || (listeners.Count == 0)) {
                // nothing to do
                return;
            }

            start();

            EventInfo eventInfo = new EventInfo(evnt, listeners);
            postEvent(eventInfo);
        }

        /*
         * go through the list of registered auto-closing
         * Clip instances and close them, if appropriate
         *
         * This method is called in regular intervals
         */
        private void closeAutoClosingClips() {
            lock (autoClosingClips) {
                long currTime = Environment.TickCount;
                for (int i = autoClosingClips.Count - 1; i >= 0; i--) {
                    ClipInfo info = autoClosingClips[i];
                    if (info.isExpired(currTime)) {
                        IAutoClosingClip clip = info.getClip();
                        // sanity check
                        if (!clip.isOpen() || !clip.isAutoClosing()) {
                            autoClosingClips.RemoveAt(i);
                        } else if (!clip.isRunning() && !clip.isActive() && clip.isAutoClosing()) {
                            clip.close();
                        } else {
                        }
                    }
                }
            }
        }

        private int getAutoClosingClipIndex(IAutoClosingClip clip) {
            lock (autoClosingClips) {
                for (int i = autoClosingClips.Count - 1; i >= 0; i--) {
                    if (clip.Equals(autoClosingClips[i].getClip())) {
                        return i;
                    }
                }
            }
            return -1;
        }

        /**
         * called from auto-closing clips when one of their open() method is called.
         */
        internal void autoClosingClipOpened(IAutoClosingClip clip) {
            int index = 0;
            lock (autoClosingClips) {
                index = getAutoClosingClipIndex(clip);
                if (index == -1) {
                    autoClosingClips.Add(new ClipInfo(clip));
                }
            }
            if (index == -1) {
                lock (this) {
                    // this is only for the case that the first clip is set to autoclosing,
                    // and it is already open, and nothing is done with it.
                    // EventDispatcher.process() method would block in wait() and
                    // never close this first clip, keeping the device open.
                    Monitor.PulseAll(this); // notifyAll();
                }
            }
        }

        /**
         * called from auto-closing clips when their closed() method is called.
         */
        internal void autoClosingClipClosed(IAutoClosingClip clip) {
            lock (autoClosingClips) {
                int index = getAutoClosingClipIndex(clip);
                if (index != -1) {
                    autoClosingClips.RemoveAt(index);
                }
            }
        }


        // ////////////////////////// Line Monitoring Support /////////////////// //
        /*
         * go through the list of registered line monitors
         * and call their checkLine method
         *
         * This method is called in regular intervals
         */
        private void monitorLines() {
            lock (lineMonitors) {
                for (int i = 0; i < lineMonitors.Count; i++) {
                    lineMonitors[i].checkLine();
                }
            }
        }

        /**
         * Add this LineMonitor instance to the list of monitors.
         */
        internal void addLineMonitor(ILineMonitor lm) {
            lock (lineMonitors) {
                if (lineMonitors.IndexOf(lm) >= 0) {
                    return;
                }
                lineMonitors.Add(lm);
            }
            lock (this) {
                // need to interrupt the infinite wait()
                Monitor.PulseAll(this); // notifyAll();
            }
        }

        /**
         * Remove this LineMonitor instance from the list of monitors.
         */
        internal void removeLineMonitor(ILineMonitor lm) {
            lock (lineMonitors) {
                if (lineMonitors.IndexOf(lm) < 0) {
                    return;
                }
                lineMonitors.Remove(lm);
            }
        }

        /**
         * Container for an event and a set of listeners to deliver it to.
         */
        internal class EventInfo {

            private readonly Object evnt;
            private readonly Object[] listeners;

            /**
             * Create a new instance of this event Info class
             * @param event the event to be dispatched
             * @param listeners listener list; will be copied
             */
            internal EventInfo(Object evnt, IList<Object> listeners) {
                this.evnt = evnt;
                this.listeners = ((List<Object>)listeners).ToArray();
            }

            internal Object getEvent() {
                return evnt;
            }

            internal int getListenerCount() {
                return listeners.Length;
            }

            internal Object getListener(int index) {
                return listeners[index];
            }

        } // class EventInfo

        /**
         * Container for a clip with its expiration time.
         */
        private class ClipInfo {

            private readonly IAutoClosingClip clip;
            private readonly long expiration;

            /**
             * Create a new instance of this clip Info class.
             */
            internal ClipInfo(IAutoClosingClip clip) {
                this.clip = clip;
                this.expiration = Environment.TickCount + AUTO_CLOSE_TIME;
            }

            internal IAutoClosingClip getClip() {
                return clip;
            }

            internal bool isExpired(long currTime) {
                return currTime > expiration;
            }
        } // class ClipInfo


        /**
         * Interface that a class that wants to get regular 
         * line monitor events implements.
         */
        internal interface ILineMonitor {
            /**
             * Called by event dispatcher in regular intervals.
             */
            void checkLine();
        }

        private void printStackTrace(Exception ex) {
            Printer.printStackTrace(ex);
        }

    } // class EventDispatcher
}
