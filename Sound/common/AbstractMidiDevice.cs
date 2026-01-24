/*
 * Copyright (c) 1999, 2025, Oracle and/or its affiliates. All rights reserved.
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
//import java.util.Collections;
//import java.util.List;

//import javax.sound.midi.InvalidMidiDataException;
//import javax.sound.midi.MidiDevice;
//import javax.sound.midi.MidiDeviceReceiver;
//import javax.sound.midi.MidiDeviceTransmitter;
//import javax.sound.midi.MidiMessage;
//import javax.sound.midi.MidiUnavailableException;
//import javax.sound.midi.Receiver;
//import javax.sound.midi.Transmitter;

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;
using SystemX.Sound.Midi;

namespace SystemX.Media.Sound {
    /**
     * Abstract AbstractMidiDevice class representing functionality shared by
     * MidiInDevice and MidiOutDevice objects.
     *
     * @author David Rivas
     * @author Kara Kytle
     * @author Matthias Pfisterer
     * @author Florian Bomers
     */
    internal abstract class AbstractMidiDevice : IMidiDevice, IReferenceCountingDevice, IDisposable { //CriticalFinalizerObject, ?

        private List<IReceiver> receiverList;

        private TransmitterList transmitterList;

        // lock to protect receiverList and transmitterList
        // from simultaneous creation and destruction
        // reduces possibility of deadlock, compared to
        // synchronizing to the class instance
        private readonly Object traRecLock = new Object();

        // DEVICE ATTRIBUTES

        private readonly MidiDevice.Info info;


        // DEVICE STATE

        private volatile bool _open;
        private int openRefCount;

        /** List of Receivers and Transmitters that opened the device implicitly.
         */
        private List<Object> openKeepingObjects;

        /**
         * This is the device handle returned from native code.
         */
        protected volatile IntPtr id;

        /**
         * Constructs an AbstractMidiDevice with the specified info object.
         * @param info the description of the device
         */
        /*
         * The initial mode and only supported mode default to OMNI_ON_POLY.
         */
        protected AbstractMidiDevice(MidiDevice.Info info) {

            this.info = info;
            openRefCount = 0;
        }

        // MIDI DEVICE METHODS

        public MidiDevice.Info getDeviceInfo() {
            return info;
        }

        /** Open the device from an application program.
         * Setting the open reference count to -1 here prevents Transmitters and Receivers that
         * opened the device implicitly from closing it. The only way to close the device after
         * this call is a call to close().
         */
        public void open() {
            lock (this) {
                openRefCount = -1;
                doOpen();
            }
        }

        /** Open the device implicitly.
         * This method is intended to be used by AbstractReceiver
         * and BasicTransmitter. Actually, it is called by getReceiverReferenceCounting() and
         * getTransmitterReferenceCounting(). These, in turn, are called by MidiSytem on calls to
         * getReceiver() and getTransmitter(). The former methods should pass the Receiver or
         * Transmitter just created as the object parameter to this method. Storing references to
         * these objects is necessary to be able to decide later (when it comes to closing) if
         * R/T's are ones that opened the device implicitly.
         *
         * @object The Receiver or Transmitter instance that triggered this implicit open.
         */
        private void openInternal(Object obj) {
            lock (this) {
                if (openRefCount != -1) {
                    openRefCount++;
                    getOpenKeepingObjects().Add(obj);
                }
                // double calls to doOpens() will be caught by the open flag.
                doOpen();
            }
        }

        private void doOpen() {
            lock (this) {
                if (!isOpen()) {
                    implOpen();
                    _open = true;
                }
            }
        }

        public void close() {
            lock (this) {
                doClose();
                openRefCount = 0;
            }
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            //if (disposing) {
                close();
            //}
        }

        /** Close the device for an object that implicitly opened it.
         * This method is intended to be used by Transmitter.close() and Receiver.close().
         * Those methods should pass this for the object parameter. Since Transmitters or Receivers
         * do not know if their device has been opened implicitly because of them, they call this
         * method in any case. This method now is able to separate Receivers/Transmitters that opened
         * the device implicitly from those that didn't by looking up the R/T in the
         * openKeepingObjects list. Only if the R/T is contained there, the reference count is
         * reduced.
         *
         * @param object The object that might have been opening the device implicitly (for now,
         * this may be a Transmitter or receiver).
         */
        public void closeInternal(Object obj) {
            lock (this) {
                if (getOpenKeepingObjects().Remove(obj)) {
                    if (openRefCount > 0) {
                        openRefCount--;
                        if (openRefCount == 0) {
                            doClose();
                        }
                    }
                }
            }
        }

        public void doClose() {
            lock (this) {
                if (isOpen()) {
                    implClose();
                    _open = false;
                }
            }
        }

        public bool isOpen() {
            return _open;
        }

        protected virtual void implClose() {
            lock (traRecLock) {
                if (receiverList != null) {
                    // close all receivers
                    for (int i = 0; i < receiverList.Count; i++) {
                        receiverList[i].close();
                    }
                    receiverList.Clear();
                }
                if (transmitterList != null) {
                    // close all transmitters
                    transmitterList.close();
                }
            }
        }

        /**
         * This implementation always returns -1.
         * Devices that actually provide this should over-ride
         * this method.
         */
        public virtual long getMicrosecondPosition() {
            return -1;
        }

        /** Return the maximum number of Receivers supported by this device.
        Depending on the return value of hasReceivers(), this method returns either 0 or -1.
        Subclasses should rather override hasReceivers() than override this method.
         */
        public int getMaxReceivers() { //sealed
            if (hasReceivers()) {
                return -1;
            } else {
                return 0;
            }
        }

        /** Return the maximum number of Transmitters supported by this device.
        Depending on the return value of hasTransmitters(), this method returns either 0 or -1.
        Subclasses should override hasTransmitters().
         */
        public int getMaxTransmitters() {//sealed 
            if (hasTransmitters()) {
                return -1;
            } else {
                return 0;
            }
        }

        /** Retrieve a Receiver for this device.
        This method returns the value returned by createReceiver(), if it doesn't throw
        an exception. Subclasses should rather override createReceiver() than override
        this method.
        If createReceiver returns a Receiver, it is added to the internal list
        of Receivers (see getReceiversList)
         */
        public IReceiver getReceiver() { //sealed 
            IReceiver receiver;
            lock (traRecLock) {
                receiver = createReceiver(); // may throw MidiUnavailableException
                getReceiverList().Add(receiver);
            }
            return receiver;
        }

        public IList<IReceiver> getReceivers() { //sealed
            IList<IReceiver> recs;
            lock (traRecLock) {
                if (receiverList == null) {
                    recs = (new List<IReceiver>(0)).AsReadOnly(); //Collections.unmodifiableList
                } else {
                    List<IReceiver> nRecs = new List<IReceiver>();
                    nRecs.AddRange(receiverList);
                    recs = nRecs.AsReadOnly();
                }
            }
            return recs;
        }

        /**
         * This implementation uses createTransmitter, which may throw an exception.
         * If a transmitter is returned in createTransmitter, it is added to the internal
         * TransmitterList
         */
        public ITransmitter getTransmitter() { //sealed
            ITransmitter transmitter;
            lock (traRecLock) {
                transmitter = createTransmitter(); // may throw MidiUnavailableException
                getTransmitterList().add(transmitter);
            }
            return transmitter;
        }

        public IList<ITransmitter> getTransmitters() { //sealed
            IList<ITransmitter> tras;
            lock (traRecLock) {
                if (transmitterList == null
                    || transmitterList.transmitters.Count == 0) {
                    tras = (new List<ITransmitter>(0).AsReadOnly()); //ICollection.unmodifiableList
                } else {
                    List<ITransmitter> nTras = new List<ITransmitter>();
                    nTras.AddRange(transmitterList.transmitters);
                    tras = nTras.AsReadOnly(); //Collections.unmodifiableList
                }
            }
            return tras;
        }

        internal IntPtr getId() {
            return id;
        }

        // REFERENCE COUNTING

        /** Retrieve a Receiver and open the device implicitly.
        This method is called by MidiSystem.getReceiver().
         */
        public IReceiver getReceiverReferenceCounting() {
            /* Keep this order of commands! If getReceiver() throws an exception,
               openInternal() should not be called!
            */
            IReceiver receiver;
            lock (traRecLock) {
                receiver = getReceiver();
                this.openInternal(receiver);
            }
            return receiver;
        }

        /** Retrieve a Transmitter and open the device implicitly.
        This method is called by MidiSystem.getTransmitter().
         */
        public ITransmitter getTransmitterReferenceCounting() {
            /* Keep this order of commands! If getTransmitter() throws an exception,
               openInternal() should not be called!
            */
            ITransmitter transmitter;
            lock (traRecLock) {
                transmitter = getTransmitter();
                this.openInternal(transmitter);
            }
            return transmitter;
        }

        /** Return the list of objects that have opened the device implicitly.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        private IList<Object> getOpenKeepingObjects() {
            if (openKeepingObjects == null) {
                openKeepingObjects = new List<Object>();
            }
            return openKeepingObjects;
        }

        // RECEIVER HANDLING METHODS

        /** Return the internal list of Receivers, possibly creating it first.
         */
        private IList<IReceiver> getReceiverList() {
            lock (traRecLock) {
                if (receiverList == null) {
                    receiverList = new List<IReceiver>();
                }
            }
            return receiverList;
        }

        /** Returns if this device supports Receivers.
        Subclasses that use Receivers should override this method to
        return true. They also should override createReceiver().

        @return true, if the device supports Receivers, false otherwise.
        */
        protected virtual bool hasReceivers() {
            return false;
        }

        /** Create a Receiver object.
        throwing an exception here means that Receivers aren't enabled.
        Subclasses that use Receivers should override this method with
        one that returns objects implementing Receiver.
        Classes overriding this method should also override hasReceivers()
        to return true.
        */
        protected virtual IReceiver createReceiver() {
            throw new MidiUnavailableException("MIDI IN receiver not available");
        }

        // TRANSMITTER HANDLING

        /** Return the internal list of Transmitters, possibly creating it first.
         */
        internal TransmitterList getTransmitterList() {
            lock (traRecLock) {
                if (transmitterList == null) {
                    transmitterList = new TransmitterList();
                }
            }
            return transmitterList;
        }

        /** Returns if this device supports Transmitters.
        Subclasses that use Transmitters should override this method to
        return true. They also should override createTransmitter().

        @return true, if the device supports Transmitters, false otherwise.
        */
        protected virtual bool hasTransmitters() {
            return false;
        }

        /** Create a Transmitter object.
        throwing an exception here means that Transmitters aren't enabled.
        Subclasses that use Transmitters should override this method with
        one that returns objects implementing Transmitters.
        Classes overriding this method should also override hasTransmitters()
        to return true.
        */
        protected virtual ITransmitter createTransmitter() {
            throw new MidiUnavailableException("MIDI OUT transmitter not available");
        }

        protected abstract void implOpen();

        /**
         * close this device if discarded by the garbage collector.
         */
        ~AbstractMidiDevice() { //protected void finalize()
            Dispose(false);
        }

        /** Base class for Receivers.
        Subclasses that use Receivers must use this base class, since it
        contains magic necessary to manage implicit closing the device.
        This is necessary for Receivers retrieved via MidiSystem.getReceiver()
        (which opens the device implicitly).
         */
        internal abstract class AbstractReceiver : IMidiDeviceReceiver {
            private volatile bool open = true;

            private AbstractMidiDevice caller;

            internal AbstractReceiver(AbstractMidiDevice caller) {
                this.caller = caller;
            }

            /** Deliver a MidiMessage.
                This method contains magic related to the closed state of a
                Receiver. Therefore, subclasses should not override this method.
                Instead, they should implement implSend().
            */
            //@Override
            [MethodImpl(MethodImplOptions.Synchronized)]
            public void send(MidiMessage message, long timeStamp) {
                if (!open) {
                    throw new InvalidOperationException("Receiver is not open"); //IllegalStateException
                }
                implSend(message, timeStamp);
            }

            internal abstract void implSend(MidiMessage message, long timeStamp);

            /** Close the Receiver.
             * Here, the call to the magic method closeInternal() takes place.
             * Therefore, subclasses that override this method must call
             * 'super.close()'.
             */
            //@Override
            public void close() {
                open = false;
                lock (caller.traRecLock) {
                    caller.getReceiverList().Remove(this);
                }
                caller.closeInternal(this);
            }

            public void Dispose() {
                close();
            }

            //@Override
            public IMidiDevice getMidiDevice() {
                return caller;
            }

            internal bool isOpen() {
                return open;
            }

        } // class AbstractReceiver

        /**
         * Transmitter base class.
         * This class especially makes sure the device is closed if it
         * has been opened implicitly by a call to MidiSystem.getTransmitter().
         * The logic of doing so is actually in closeInternal().
         *
         * Also, it has some optimizations regarding sending to the Receivers,
         * for known Receivers, and managing itself in the TransmitterList.
         */
        internal class BasicTransmitter : IMidiDeviceTransmitter {

            private IReceiver receiver = null;
            protected TransmitterList tlist = null;

            private AbstractMidiDevice caller;

            internal BasicTransmitter(AbstractMidiDevice caller) {
                this.caller = caller;
            }

            internal void setTransmitterList(TransmitterList tlist) {
                this.tlist = tlist;
            }

            public void setReceiver(IReceiver receiver) {
                if (tlist != null && this.receiver != receiver) {
                    tlist.receiverChanged(this, this.receiver, receiver);
                    this.receiver = receiver;
                }
            }

            public IReceiver getReceiver() {
                return receiver;
            }

            /** Close the Transmitter.
             * Here, the call to the magic method closeInternal() takes place.
             * Therefore, subclasses that override this method must call
             * 'super.close()'.
             */
            public void close() {
                caller.closeInternal(this);
                if (tlist != null) {
                    tlist.receiverChanged(this, this.receiver, null);
                    tlist.remove(this);
                    tlist = null;
                }
            }

            public void Dispose() {
                close();
            }

            public IMidiDevice getMidiDevice() {
                return caller;
            }
        } // class BasicTransmitter

        /**
         * a class to manage a list of transmitters.
         */
        internal sealed class TransmitterList {

            internal readonly IList<ITransmitter> transmitters = new List<ITransmitter>();
            private MidiOutDevice.MidiOutReceiver midiOutReceiver;

            // how many transmitters must be present for optimized
            // handling
            private int optimizedReceiverCount = 0;

            internal TransmitterList() {
            }

            internal void add(ITransmitter t) {
                lock (transmitters) {
                    transmitters.Add(t);
                }
                if (t is BasicTransmitter) {
                    ((BasicTransmitter)t).setTransmitterList(this);
                }
            }

            internal void remove(ITransmitter t) {
                lock (transmitters) {
                    transmitters.Remove(t);
                }
            }

            internal void receiverChanged(BasicTransmitter t,
                                         IReceiver oldR,
                                         IReceiver newR) {
                lock (transmitters) {
                    // some optimization
                    if (midiOutReceiver == oldR) {
                        midiOutReceiver = null;
                    }
                    if ((newR is MidiOutDevice.MidiOutReceiver newReceiver)
                        && (midiOutReceiver == null)) {
                        midiOutReceiver = newReceiver;
                    }
                    optimizedReceiverCount =
                          ((midiOutReceiver != null) ? 1 : 0);
                }
                // more potential for optimization here
            }


            /** closes all transmitters and empties the list */
            internal void close() {
                lock (transmitters) {
                    for (int i = 0; i < transmitters.Count; i++) {
                        transmitters[i].close();
                    }
                    transmitters.Clear();
                }
            }


            /**
            * Send this message to all receivers
            * status = packedMessage & 0xFF
            * data1 = (packedMessage & 0xFF00) >> 8;
            * data1 = (packedMessage & 0xFF0000) >> 16;
            */
            internal void sendMessage(int packedMessage, long timeStamp) {
                try {
                    lock (transmitters) {
                        int size = transmitters.Count;
                        if (optimizedReceiverCount == size) {
                            if (midiOutReceiver != null) {
                                midiOutReceiver.sendPackedMidiMessage(packedMessage, timeStamp);
                            }
                        } else {
                            for (int i = 0; i < size; i++) {
                                IReceiver receiver = transmitters[i].getReceiver();
                                if (receiver != null) {
                                    if (optimizedReceiverCount > 0) {
                                        if (receiver is MidiOutDevice.MidiOutReceiver) {
                                            ((MidiOutDevice.MidiOutReceiver)receiver).sendPackedMidiMessage(packedMessage, timeStamp);
                                        } else {
                                            receiver.send(new FastShortMessage(packedMessage), timeStamp);
                                        }
                                    } else {
                                        receiver.send(new FastShortMessage(packedMessage), timeStamp);
                                    }
                                }
                            }
                        }
                    }
                } catch (InvalidMidiDataException) {
                    // this happens when invalid data comes over the wire. Ignore it.
                }
            }

            internal void sendMessage(byte[] data, long timeStamp) {
                try {
                    lock (transmitters) {
                        int size = transmitters.Count;
                        for (int i = 0; i < size; i++) {
                            IReceiver receiver = transmitters[i].getReceiver();
                            if (receiver != null) {
                                //$$fb 2002-04-02: SysexMessages are mutable, so
                                // an application could change the contents of this object,
                                // or try to use the object later. So we can't get around object creation
                                // But the array need not be unique for each FastSysexMessage object,
                                // because it cannot be modified.
                                receiver.send(new FastSysexMessage(data), timeStamp);
                            }
                        }
                    }
                } catch (InvalidMidiDataException) {
                    // this happens when invalid data comes over the wire. Ignore it.
                    return;
                }
            }

            /**
            * Send this message to all transmitters.
            */
            internal void sendMessage(MidiMessage message, long timeStamp) {
                if (message is FastShortMessage) {
                    sendMessage(((FastShortMessage)message).getPackedMsg(), timeStamp);
                    return;
                }
                lock (transmitters) {
                    int size = transmitters.Count;
                    if (optimizedReceiverCount == size) {
                        if (midiOutReceiver != null) {
                            midiOutReceiver.send(message, timeStamp);
                        }
                    } else {
                        for (int i = 0; i < size; i++) {
                            IReceiver receiver = ((ITransmitter)transmitters[i]).getReceiver();
                            if (receiver != null) {
                                //$$fb 2002-04-02: ShortMessages are mutable, so
                                // an application could change the contents of this object,
                                // or try to use the object later.
                                // We violate this spec here, to avoid costly (and gc-intensive)
                                // object creation for potentially hundred of messages per second.
                                // The spec should be changed to allow Immutable MidiMessages
                                // (i.e. throws InvalidStateException or so in setMessage)
                                receiver.send(message, timeStamp);
                            }
                        }
                    }
                }
            }
        } // TransmitterList
    }
}
