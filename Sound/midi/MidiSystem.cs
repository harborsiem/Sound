/*
 * Copyright (c) 1999, 2020, Oracle and/or its affiliates. All rights reserved.
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

//package javax.sound.midi;

//import java.io.File;
//import java.io.IOException;
//import java.io.InputStream;
//import java.io.OutputStream;
//import java.net.URL;
//import java.util.ArrayList;
//import java.util.Collections;
//import java.util.HashSet;
//import java.util.Iterator;
//import java.util.List;
//import java.util.Properties;
//import java.util.Set;

//import javax.sound.midi.spi.MidiDeviceProvider;
//import javax.sound.midi.spi.MidiFileReader;
//import javax.sound.midi.spi.MidiFileWriter;
//import javax.sound.midi.spi.SoundbankReader;

//import com.sun.media.sound.AutoConnectSequencer;
//import com.sun.media.sound.JDK13Services;
//import com.sun.media.sound.MidiDeviceReceiverEnvelope;
//import com.sun.media.sound.MidiDeviceTransmitterEnvelope;
//import com.sun.media.sound.ReferenceCountingDevice;

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.IO;
using SystemX.Media.Sound;

namespace SystemX.Sound.Midi {
/**
 * The {@code MidiSystem} class provides access to the installed MIDI system
 * resources, including devices such as synthesizers, sequencers, and MIDI input
 * and output ports. A typical simple MIDI application might begin by invoking
 * one or more {@code MidiSystem} methods to learn what devices are installed
 * and to obtain the ones needed in that application.
 * <p>
 * The class also has methods for reading files, streams, and URLs that contain
 * standard MIDI file data or soundbanks. You can query the {@code MidiSystem}
 * for the format of a specified MIDI file.
 * <p>
 * You cannot instantiate a {@code MidiSystem}; all the methods are static.
 * <p>
 * Properties can be used to specify default MIDI devices. Both system
 * properties and a properties file are considered. The "sound.properties"
 * properties file is read from an implementation-specific location (typically
 * it is the {@code conf} directory in the Java installation directory).
 * The optional "javax.sound.config.file" system property can be used to specify
 * the properties file that will be read as the initial configuration. If a
 * property exists both as a system property and in the properties file, the
 * system property takes precedence. If none is specified, a suitable default is
 * chosen among the available devices. The syntax of the properties file is
 * specified in {@link Properties#load(InputStream) Properties.load}. The
 * following table lists the available property keys and which methods consider
 * them:
 *
 * <table class="striped">
 * <caption>MIDI System Property Keys</caption>
 * <thead>
 *   <tr>
 *     <th scope="col">Property Key
 *     <th scope="col">Interface
 *     <th scope="col">Affected Method
 * </thead>
 * <tbody>
 *   <tr>
 *     <th scope="row">{@code javax.sound.midi.Receiver}
 *     <td>{@link Receiver}
 *     <td>{@link #getReceiver}
 *   <tr>
 *     <th scope="row">{@code javax.sound.midi.Sequencer}
 *     <td>{@link Sequencer}
 *     <td>{@link #getSequencer}
 *   <tr>
 *     <th scope="row">{@code javax.sound.midi.Synthesizer}
 *     <td>{@link Synthesizer}
 *     <td>{@link #getSynthesizer}
 *   <tr>
 *     <th scope="row">{@code javax.sound.midi.Transmitter}
 *     <td>{@link Transmitter}
 *     <td>{@link #getTransmitter}
 * </tbody>
 * </table>
 *
 * The property value consists of the provider class name and the device name,
 * separated by the hash mark ("#"). The provider class name is the
 * fully-qualified name of a concrete
 * {@link MidiDeviceProvider MIDI device provider} class. The device name is
 * matched against the {@code String} returned by the {@code getName} method of
 * {@code MidiDevice.Info}. Either the class name, or the device name may be
 * omitted. If only the class name is specified, the trailing hash mark is
 * optional.
 * <p>
 * If the provider class is specified, and it can be successfully retrieved from
 * the installed providers, the list of {@code MidiDevice.Info} objects is
 * retrieved from the provider. Otherwise, or when these devices do not provide
 * a subsequent match, the list is retrieved from {@link #getMidiDeviceInfo} to
 * contain all available {@code MidiDevice.Info} objects.
 * <p>
 * If a device name is specified, the resulting list of {@code MidiDevice.Info}
 * objects is searched: the first one with a matching name, and whose
 * {@code MidiDevice} implements the respective interface, will be returned. If
 * no matching {@code MidiDevice.Info} object is found, or the device name is
 * not specified, the first suitable device from the resulting list will be
 * returned. For Sequencer and Synthesizer, a device is suitable if it
 * implements the respective interface; whereas for Receiver and Transmitter, a
 * device is suitable if it implements neither Sequencer nor Synthesizer and
 * provides at least one Receiver or Transmitter, respectively.
 * <p>
 * For example, the property {@code javax.sound.midi.Receiver} with a value
 * {@code "com.sun.media.sound.MidiProvider#SunMIDI1"} will have the following
 * consequences when {@code getReceiver} is called: if the class
 * {@code com.sun.media.sound.MidiProvider} exists in the list of installed MIDI
 * device providers, the first {@code Receiver} device with name
 * {@code "SunMIDI1"} will be returned. If it cannot be found, the first
 * {@code Receiver} from that provider will be returned, regardless of name. If
 * there is none, the first {@code Receiver} with name {@code "SunMIDI1"} in the
 * list of all devices (as returned by {@code getMidiDeviceInfo}) will be
 * returned, or, if not found, the first {@code Receiver} that can be found in
 * the list of all devices is returned. If that fails, too, a
 * {@code MidiUnavailableException} is thrown.
 *
 * @author Kara Kytle
 * @author Florian Bomers
 * @author Matthias Pfisterer
 */
    public sealed class MidiSystem {

        /**
         * Private no-args constructor for ensuring against instantiation.
         */
        private MidiSystem() {
        }

        /**
         * Obtains an array of information objects representing the set of all MIDI
         * devices available on the system. A returned information object can then
         * be used to obtain the corresponding device object, by invoking
         * {@link #getMidiDevice(MidiDevice.Info) getMidiDevice}.
         *
         * @return an array of {@code MidiDevice.Info} objects, one for each
         *         installed MIDI device. If no such devices are installed, an array
         *         of length 0 is returned.
         */
        public static MidiDevice.Info[] getMidiDeviceInfo() {
            List<MidiDevice.Info> allInfos = new List<MidiDevice.Info>();
            foreach (MidiDeviceProvider provider in getMidiDeviceProviders()) {
                MidiDevice.Info[] tmpinfo = provider.getDeviceInfo();
                for (int j = 0; j < tmpinfo.Length; j++) {
                    allInfos.Add(tmpinfo[j]);
                }
            }
            //IList<MidiDeviceProvider> providers = getMidiDeviceProviders();

            //for (int i = 0; i < providers.Count; i++) {
            //    MidiDeviceProvider provider = providers[i];
            //    MidiDevice.Info[] tmpinfo = provider.getDeviceInfo();
            //    for (int j = 0; j < tmpinfo.Length; j++) {
            //        allInfos.Add(tmpinfo[j]);
            //    }
            //}
            return allInfos.ToArray();
        }

        /**
         * Obtains the requested MIDI device.
         *
         * @param  info a device information object representing the desired device
         * @return the requested device
         * @throws MidiUnavailableException if the requested device is not available
         *         due to resource restrictions
         * @throws IllegalArgumentException if the info object does not represent a
         *         MIDI device installed on the system
         * @throws NullPointerException if {@code info} is {@code null}
         * @see #getMidiDeviceInfo
         */
        public static IMidiDevice getMidiDevice(MidiDevice.Info info) {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            foreach (MidiDeviceProvider provider in getMidiDeviceProviders()) {
                if (provider.isDeviceSupported(info)) {
                    return provider.getDevice(info);
                }
            }
            //IList<MidiDeviceProvider> providers = getMidiDeviceProviders();

            //for (int i = 0; i < providers.Count; i++) {
            //    MidiDeviceProvider provider = (MidiDeviceProvider)providers[i];
            //    if (provider.isDeviceSupported(info)) {
            //        return provider.getDevice(info);
            //    }
            //}
            throw new ArgumentException(String.Format("Requested device not installed: {0}", info));
        }

        /**
         * Obtains a MIDI receiver from an external MIDI port or other default
         * device. The returned receiver always implements the
         * {@code MidiDeviceReceiver} interface.
         * <p>
         * If the system property {@code javax.sound.midi.Receiver} is defined or it
         * is defined in the file "sound.properties", it is used to identify the
         * device that provides the default receiver. For details, refer to the
         * {@link MidiSystem class description}.
         * <p>
         * If a suitable MIDI port is not available, the Receiver is retrieved from
         * an installed synthesizer.
         * <p>
         * If a native receiver provided by the default device does not implement
         * the {@code MidiDeviceReceiver} interface, it will be wrapped in a wrapper
         * class that implements the {@code MidiDeviceReceiver} interface. The
         * corresponding {@code Receiver} method calls will be forwarded to the
         * native receiver.
         * <p>
         * If this method returns successfully, the {@link MidiDevice MidiDevice}
         * the {@code Receiver} belongs to is opened implicitly, if it is not
         * already open. It is possible to close an implicitly opened device by
         * calling {@link Receiver#close close} on the returned {@code Receiver}.
         * All open {@code Receiver} instances have to be closed in order to release
         * system resources hold by the {@code MidiDevice}. For a detailed
         * description of open/close behaviour see the class description of
         * {@link MidiDevice MidiDevice}.
         *
         * @return the default MIDI receiver
         * @throws MidiUnavailableException if the default receiver is not available
         *         due to resource restrictions, or no device providing receivers is
         *         installed in the system
         */
        public static IReceiver getReceiver() {
            // may throw MidiUnavailableException
            IMidiDevice device = getDefaultDeviceWrapper(typeof(IReceiver));
            IReceiver receiver;
            if (device is IReferenceCountingDevice) {
                receiver = ((IReferenceCountingDevice)device).getReceiverReferenceCounting();
            } else {
                receiver = device.getReceiver();
            }
            if (!(receiver is IMidiDeviceReceiver)) {
                receiver = new MidiDeviceReceiverEnvelope(device, receiver);
            }
            return receiver;
        }

        /**
         * Obtains a MIDI transmitter from an external MIDI port or other default
         * source. The returned transmitter always implements the
         * {@code MidiDeviceTransmitter} interface.
         * <p>
         * If the system property {@code javax.sound.midi.Transmitter} is defined or
         * it is defined in the file "sound.properties", it is used to identify the
         * device that provides the default transmitter. For details, refer to the
         * {@link MidiSystem class description}.
         * <p>
         * If a native transmitter provided by the default device does not implement
         * the {@code MidiDeviceTransmitter} interface, it will be wrapped in a
         * wrapper class that implements the {@code MidiDeviceTransmitter}
         * interface. The corresponding {@code Transmitter} method calls will be
         * forwarded to the native transmitter.
         * <p>
         * If this method returns successfully, the {@link MidiDevice MidiDevice}
         * the {@code Transmitter} belongs to is opened implicitly, if it is not
         * already open. It is possible to close an implicitly opened device by
         * calling {@link Transmitter#close close} on the returned
         * {@code Transmitter}. All open {@code Transmitter} instances have to be
         * closed in order to release system resources hold by the
         * {@code MidiDevice}. For a detailed description of open/close behaviour
         * see the class description of {@link MidiDevice MidiDevice}.
         *
         * @return the default MIDI transmitter
         * @throws MidiUnavailableException if the default transmitter is not
         *         available due to resource restrictions, or no device providing
         *         transmitters is installed in the system
         */
        public static ITransmitter getTransmitter() {
            // may throw MidiUnavailableException
            IMidiDevice device = getDefaultDeviceWrapper(typeof(ITransmitter));
            ITransmitter transmitter;
            if (device is IReferenceCountingDevice) {
                transmitter = ((IReferenceCountingDevice)device).getTransmitterReferenceCounting();
            } else {
                transmitter = device.getTransmitter();
            }
            if (!(transmitter is IMidiDeviceTransmitter)) {
                transmitter = new MidiDeviceTransmitterEnvelope(device, transmitter);
            }
            return transmitter;
        }

        /**
         * Obtains the default synthesizer.
         * <p>
         * If the system property {@code javax.sound.midi.Synthesizer} is defined or
         * it is defined in the file "sound.properties", it is used to identify the
         * default synthesizer. For details, refer to the
         * {@link MidiSystem class description}.
         *
         * @return the default synthesizer
         * @throws MidiUnavailableException if the synthesizer is not available due
         *         to resource restrictions, or no synthesizer is installed in the
         *         system
         */
        public static ISynthesizer getSynthesizer() {
            // may throw MidiUnavailableException
            return (ISynthesizer)getDefaultDeviceWrapper(typeof(ISynthesizer));
        }

        /**
         * Obtains the default {@code Sequencer}, connected to a default device. The
         * returned {@code Sequencer} instance is connected to the default
         * {@code Synthesizer}, as returned by {@link #getSynthesizer}. If there is
         * no {@code Synthesizer} available, or the default {@code Synthesizer}
         * cannot be opened, the {@code sequencer} is connected to the default
         * {@code Receiver}, as returned by {@link #getReceiver}. The connection is
         * made by retrieving a {@code Transmitter} instance from the
         * {@code Sequencer} and setting its {@code Receiver}. Closing and
         * re-opening the sequencer will restore the connection to the default
         * device.
         * <p>
         * This method is equivalent to calling {@code getSequencer(true)}.
         * <p>
         * If the system property {@code javax.sound.midi.Sequencer} is defined or
         * it is defined in the file "sound.properties", it is used to identify the
         * default sequencer. For details, refer to the
         * {@link MidiSystem class description}.
         *
         * @return the default sequencer, connected to a default Receiver
         * @throws MidiUnavailableException if the sequencer is not available due to
         *         resource restrictions, or there is no {@code Receiver} available
         *         by any installed {@code MidiDevice}, or no sequencer is installed
         *         in the system
         * @see #getSequencer(boolean)
         * @see #getSynthesizer
         * @see #getReceiver
         */
        public static ISequencer getSequencer() {
            return getSequencer(true);
        }

        /**
         * Obtains the default {@code Sequencer}, optionally connected to a default
         * device.
         * <p>
         * If {@code connected} is true, the returned {@code Sequencer} instance is
         * connected to the default {@code Synthesizer}, as returned by
         * {@link #getSynthesizer}. If there is no {@code Synthesizer} available, or
         * the default {@code Synthesizer} cannot be opened, the {@code sequencer}
         * is connected to the default {@code Receiver}, as returned by
         * {@link #getReceiver}. The connection is made by retrieving a
         * {@code Transmitter} instance from the {@code Sequencer} and setting its
         * {@code Receiver}. Closing and re-opening the sequencer will restore the
         * connection to the default device.
         * <p>
         * If {@code connected} is false, the returned {@code Sequencer} instance is
         * not connected, it has no open {@code Transmitters}. In order to play the
         * sequencer on a MIDI device, or a {@code Synthesizer}, it is necessary to
         * get a {@code Transmitter} and set its {@code Receiver}.
         * <p>
         * If the system property {@code javax.sound.midi.Sequencer} is defined or
         * it is defined in the file "sound.properties", it is used to identify the
         * default sequencer. For details, refer to the
         * {@link MidiSystem class description}.
         *
         * @param  connected whether or not the returned {@code Sequencer} is
         *         connected to the default {@code Synthesizer}
         * @return the default sequencer
         * @throws MidiUnavailableException if the sequencer is not available due to
         *         resource restrictions, or no sequencer is installed in the
         *         system, or if {@code connected} is true, and there is no
         *         {@code Receiver} available by any installed {@code MidiDevice}
         * @see #getSynthesizer
         * @see #getReceiver
         * @since 1.5
         */
        public static ISequencer getSequencer(bool connected) {
            ISequencer seq = (ISequencer)getDefaultDeviceWrapper(typeof(ISequencer));

            if (connected) {
                // IMPORTANT: this code needs to be synch'ed with 
                //            all AutoConnectSequencer instances,
                //            (e.g. RealTimeSequencer) because the 
                //            same algorithm for synth retrieval
                //            needs to be used!

                IReceiver rec = null;
                MidiUnavailableException mue = null;

                // first try to connect to the default synthesizer
                try {
                    ISynthesizer synth = getSynthesizer();
                    if (synth is IReferenceCountingDevice) {
                        rec = ((IReferenceCountingDevice)synth).getReceiverReferenceCounting();
                    } else {
                        synth.open();
                        try {
                            rec = synth.getReceiver();
                        } finally {
                            // make sure that the synth is properly closed
                            if (rec == null) {
                                synth.close();
                            }
                        }
                    }
                } catch (MidiUnavailableException e) {
                    // something went wrong with synth
                    if (e is MidiUnavailableException) {
                        mue = (MidiUnavailableException)e;
                    }
                }
                if (rec == null) {
                    // then try to connect to the default Receiver
                    try {
                        rec = MidiSystem.getReceiver();
                    } catch (Exception e) {
                        // something went wrong. Nothing to do then!
                        if (e is MidiUnavailableException) {
                            mue = (MidiUnavailableException)e;
                        }
                    }
                }
                if (rec != null) {
                    seq.getTransmitter().setReceiver(rec);
                    if (seq is IAutoConnectSequencer) {
                        ((IAutoConnectSequencer)seq).setAutoConnect(rec);
                    }
                } else {
                    if (mue != null) {
                        throw mue;
                    }
                    throw new MidiUnavailableException("no receiver available");
                }
            }
            return seq;
        }

        /**
         * Constructs a MIDI sound bank by reading it from the specified stream. The
         * stream must point to a valid MIDI soundbank file. In general, MIDI
         * soundbank providers may need to read some data from the stream before
         * determining whether they support it. These parsers must be able to mark
         * the stream, read enough data to determine whether they support the
         * stream, and, if not, reset the stream's read pointer to its original
         * position. If the input stream does not support this, this method may fail
         * with an {@code IOException}.
         *
         * @param  stream the source of the sound bank data
         * @return the sound bank
         * @throws InvalidMidiDataException if the stream does not point to valid
         *         MIDI soundbank data recognized by the system
         * @throws IOException if an I/O error occurred when loading the soundbank
         * @throws NullPointerException if {@code stream} is {@code null}
         * @see InputStream#markSupported
         * @see InputStream#mark
         */
        public static ISoundbank getSoundbank(Stream stream) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            SoundbankReader sp = null;
            ISoundbank s = null;

            IList<SoundbankReader> providers = getSoundbankReaders();

            for (int i = 0; i < providers.Count; i++) {
                sp = providers[i];
                s = sp.getSoundbank(stream);

                if (s != null) {
                    return s;
                }
            }
            throw new InvalidMidiDataException("cannot get soundbank from stream");

        }

        /**
         * Constructs a {@code Soundbank} by reading it from the specified URL. The
         * URL must point to a valid MIDI soundbank file.
         *
         * @param  url the source of the sound bank data
         * @return the sound bank
         * @throws InvalidMidiDataException if the URL does not point to valid MIDI
         *         soundbank data recognized by the system
         * @throws IOException if an I/O error occurred when loading the soundbank
         * @throws NullPointerException if {@code url} is {@code null}
         */
        public static ISoundbank getSoundbank(Uri url) {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            SoundbankReader sp = null;
            ISoundbank s = null;

            IList<SoundbankReader> providers = getSoundbankReaders();

            for (int i = 0; i < providers.Count; i++) {
                sp = providers[i];
                s = sp.getSoundbank(url);

                if (s != null) {
                    return s;
                }
            }
            throw new InvalidMidiDataException("cannot get soundbank from stream");

        }

        /**
         * Constructs a {@code Soundbank} by reading it from the specified
         * {@code File}. The {@code File} must point to a valid MIDI soundbank file.
         *
         * @param  file the source of the sound bank data
         * @return the sound bank
         * @throws InvalidMidiDataException if the {@code File} does not point to
         *         valid MIDI soundbank data recognized by the system
         * @throws IOException if an I/O error occurred when loading the soundbank
         * @throws NullPointerException if {@code file} is {@code null}
         */
        public static ISoundbank getSoundbank(FileInfo file) {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            SoundbankReader sp = null;
            ISoundbank s = null;

            IList<SoundbankReader> providers = getSoundbankReaders();

            for (int i = 0; i < providers.Count; i++) {
                sp = providers[i];
                s = sp.getSoundbank(file);

                if (s != null) {
                    return s;
                }
            }
            throw new InvalidMidiDataException("cannot get soundbank from stream");
        }

        /**
         * Obtains the MIDI file format of the data in the specified input stream.
         * The stream must point to valid MIDI file data for a file type recognized
         * by the system.
         * <p>
         * This method and/or the code it invokes may need to read some data from
         * the stream to determine whether its data format is supported. The
         * implementation may therefore need to mark the stream, read enough data to
         * determine whether it is in a supported format, and reset the stream's
         * read pointer to its original position. If the input stream does not
         * permit this set of operations, this method may fail with an
         * {@code IOException}.
         * <p>
         * This operation can only succeed for files of a type which can be parsed
         * by an installed file reader. It may fail with an
         * {@code InvalidMidiDataException} even for valid files if no compatible
         * file reader is installed. It will also fail with an
         * {@code InvalidMidiDataException} if a compatible file reader is
         * installed, but encounters errors while determining the file format.
         *
         * @param  stream the input stream from which file format information should
         *         be extracted
         * @return an {@code MidiFileFormat} object describing the MIDI file format
         * @throws InvalidMidiDataException if the stream does not point to valid
         *         MIDI file data recognized by the system
         * @throws IOException if an I/O exception occurs while accessing the stream
         * @throws NullPointerException if {@code stream} is {@code null}
         * @see #getMidiFileFormat(URL)
         * @see #getMidiFileFormat(File)
         * @see InputStream#markSupported
         * @see InputStream#mark
         */
        public static MidiFileFormat getMidiFileFormat(Stream stream) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            IList<MidiFileReader> providers = getMidiFileReaders();
            MidiFileFormat format = null;

            for (int i = 0; i < providers.Count; i++) {
                MidiFileReader reader = providers[i];
                try {
                    format = reader.getMidiFileFormat(stream); // throws IOException
                    break;
                } catch (InvalidMidiDataException) {
                    continue;
                }
            }

            if (format == null) {
                throw new InvalidMidiDataException("input stream is not a supported file type");
            } else {
                return format;
            }
        }

        /**
         * Obtains the MIDI file format of the data in the specified URL. The URL
         * must point to valid MIDI file data for a file type recognized by the
         * system.
         * <p>
         * This operation can only succeed for files of a type which can be parsed
         * by an installed file reader. It may fail with an
         * {@code InvalidMidiDataException} even for valid files if no compatible
         * file reader is installed. It will also fail with an
         * {@code InvalidMidiDataException} if a compatible file reader is
         * installed, but encounters errors while determining the file format.
         *
         * @param  url the URL from which file format information should be
         *         extracted
         * @return a {@code MidiFileFormat} object describing the MIDI file format
         * @throws InvalidMidiDataException if the URL does not point to valid MIDI
         *         file data recognized by the system
         * @throws IOException if an I/O exception occurs while accessing the URL
         * @throws NullPointerException if {@code url} is {@code null}
         * @see #getMidiFileFormat(InputStream)
         * @see #getMidiFileFormat(File)
         */
        public static MidiFileFormat getMidiFileFormat(Uri url) {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            IList<MidiFileReader> providers = getMidiFileReaders();
            MidiFileFormat format = null;

            for (int i = 0; i < providers.Count; i++) {
                MidiFileReader reader = providers[i];
                try {
                    format = reader.getMidiFileFormat(url); // throws IOException
                    break;
                } catch (InvalidMidiDataException) {
                    continue;
                }
            }

            if (format == null) {
                throw new InvalidMidiDataException("url is not a supported file type");
            } else {
                return format;
            }
        }

        /**
         * Obtains the MIDI file format of the specified {@code File}. The
         * {@code File} must point to valid MIDI file data for a file type
         * recognized by the system.
         * <p>
         * This operation can only succeed for files of a type which can be parsed
         * by an installed file reader. It may fail with an
         * {@code InvalidMidiDataException} even for valid files if no compatible
         * file reader is installed. It will also fail with an
         * {@code InvalidMidiDataException} if a compatible file reader is
         * installed, but encounters errors while determining the file format.
         *
         * @param  file the {@code File} from which file format information should
         *         be extracted
         * @return a {@code MidiFileFormat} object describing the MIDI file format
         * @throws InvalidMidiDataException if the {@code File} does not point to
         *         valid MIDI file data recognized by the system
         * @throws IOException if an I/O exception occurs while accessing the file
         * @throws NullPointerException if {@code file} is {@code null}
         * @see #getMidiFileFormat(InputStream)
         * @see #getMidiFileFormat(URL)
         */
        public static MidiFileFormat getMidiFileFormat(FileInfo file) {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            IList<MidiFileReader> providers = getMidiFileReaders();
            MidiFileFormat format = null;

            for (int i = 0; i < providers.Count; i++) {
                MidiFileReader reader = providers[i];
                try {
                    format = reader.getMidiFileFormat(file); // throws IOException
                    break;
                } catch (InvalidMidiDataException) {
                    continue;
                }
            }

            if (format == null) {
                throw new InvalidMidiDataException("file is not a supported file type");
            } else {
                return format;
            }
        }

        /**
         * Obtains a MIDI sequence from the specified input stream. The stream must
         * point to valid MIDI file data for a file type recognized by the system.
         * <p>
         * This method and/or the code it invokes may need to read some data from
         * the stream to determine whether its data format is supported. The
         * implementation may therefore need to mark the stream, read enough data to
         * determine whether it is in a supported format, and reset the stream's
         * read pointer to its original position. If the input stream does not
         * permit this set of operations, this method may fail with an
         * {@code IOException}.
         * <p>
         * This operation can only succeed for files of a type which can be parsed
         * by an installed file reader. It may fail with an
         * {@code InvalidMidiDataException} even for valid files if no compatible
         * file reader is installed. It will also fail with an
         * {@code InvalidMidiDataException} if a compatible file reader is
         * installed, but encounters errors while constructing the {@code Sequence}
         * object from the file data.
         *
         * @param  stream the input stream from which the {@code Sequence} should be
         *         constructed
         * @return a {@code Sequence} object based on the MIDI file data contained
         *         in the input stream
         * @throws InvalidMidiDataException if the stream does not point to valid
         *         MIDI file data recognized by the system
         * @throws IOException if an I/O exception occurs while accessing the stream
         * @throws NullPointerException if {@code stream} is {@code null}
         * @see InputStream#markSupported
         * @see InputStream#mark
         */
        public static Sequence getSequence(Stream stream) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            IList<MidiFileReader> providers = getMidiFileReaders();
            Sequence sequence = null;

            for (int i = 0; i < providers.Count; i++) {
                MidiFileReader reader = providers[i];
                try {
                    sequence = reader.getSequence(stream); // throws IOException
                    break;
                } catch (InvalidMidiDataException) {
                    continue;
                }
            }

            if (sequence == null) {
                throw new InvalidMidiDataException("could not get sequence from input stream");
            } else {
                return sequence;
            }
        }

        /**
         * Obtains a MIDI sequence from the specified URL. The URL must point to
         * valid MIDI file data for a file type recognized by the system.
         * <p>
         * This operation can only succeed for files of a type which can be parsed
         * by an installed file reader. It may fail with an
         * {@code InvalidMidiDataException} even for valid files if no compatible
         * file reader is installed. It will also fail with an
         * {@code InvalidMidiDataException} if a compatible file reader is
         * installed, but encounters errors while constructing the {@code Sequence}
         * object from the file data.
         *
         * @param  url the URL from which the {@code Sequence} should be constructed
         * @return a {@code Sequence} object based on the MIDI file data pointed to
         *         by the URL
         * @throws InvalidMidiDataException if the URL does not point to valid MIDI
         *         file data recognized by the system
         * @throws IOException if an I/O exception occurs while accessing the URL
         * @throws NullPointerException if {@code url} is {@code null}
         */
        public static Sequence getSequence(Uri url) {
            if (url == null)
                throw new ArgumentNullException(nameof(url));

            IList<MidiFileReader> providers = getMidiFileReaders();
            Sequence sequence = null;

            for (int i = 0; i < providers.Count; i++) {
                MidiFileReader reader = providers[i];
                try {
                    sequence = reader.getSequence(url); // throws IOException
                    break;
                } catch (InvalidMidiDataException) {
                    continue;
                }
            }

            if (sequence == null) {
                throw new InvalidMidiDataException("could not get sequence from URL");
            } else {
                return sequence;
            }
        }

        /**
         * Obtains a MIDI sequence from the specified {@code File}. The {@code File}
         * must point to valid MIDI file data for a file type recognized by the
         * system.
         * <p>
         * This operation can only succeed for files of a type which can be parsed
         * by an installed file reader. It may fail with an
         * {@code InvalidMidiDataException} even for valid files if no compatible
         * file reader is installed. It will also fail with an
         * {@code InvalidMidiDataException} if a compatible file reader is
         * installed, but encounters errors while constructing the {@code Sequence}
         * object from the file data.
         *
         * @param  file the {@code File} from which the {@code Sequence} should be
         *         constructed
         * @return a {@code Sequence} object based on the MIDI file data pointed to
         *         by the File
         * @throws InvalidMidiDataException if the File does not point to valid MIDI
         *         file data recognized by the system
         * @throws IOException if an I/O exception occurs
         * @throws NullPointerException if {@code file} is {@code null}
         */
        public static Sequence getSequence(FileInfo file) {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            IList<MidiFileReader> providers = getMidiFileReaders();
            Sequence sequence = null;

            for (int i = 0; i < providers.Count; i++) {
                MidiFileReader reader = providers[i];
                try {
                    sequence = reader.getSequence(file); // throws IOException
                    break;
                } catch (InvalidMidiDataException) {
                    continue;
                }
            }

            if (sequence == null) {
                throw new InvalidMidiDataException("could not get sequence from file");
            } else {
                return sequence;
            }
        }

        /**
         * Obtains the set of MIDI file types for which file writing support is
         * provided by the system.
         *
         * @return array of unique file types. If no file types are supported, an
         *         array of length 0 is returned.
         */
        public static int[] getMidiFileTypes() {

            IList<MidiFileWriter> providers = getMidiFileWriters();
            HashSet<Int32> allTypes = new HashSet<Int32>();

            // gather from all the providers

            for (int i = 0; i < providers.Count; i++) {
                MidiFileWriter writer = providers[i];
                int[] types = writer.getMidiFileTypes();
                for (int j = 0; j < types.Length; j++) {
                    allTypes.Add(types[j]);
                }
            }
            int[] resultTypes = new int[allTypes.Count];
            int index = 0;
            foreach (Int32 integer in allTypes) {
                resultTypes[index++] = integer;
            }
            return resultTypes;
        }

        /**
         * Indicates whether file writing support for the specified MIDI file type
         * is provided by the system.
         *
         * @param  fileType the file type for which write capabilities are queried
         * @return {@code true} if the file type is supported, otherwise
         *         {@code false}
         */
        public static bool isFileTypeSupported(int fileType) {

            IList<MidiFileWriter> providers = getMidiFileWriters();

            for (int i = 0; i < providers.Count; i++) {
                MidiFileWriter writer = providers[i];
                if (writer.isFileTypeSupported(fileType)) {
                    return true;
                }
            }
            return false;
        }

        /**
         * Obtains the set of MIDI file types that the system can write from the
         * sequence specified.
         *
         * @param  sequence the sequence for which MIDI file type support is queried
         * @return the set of unique supported file types. If no file types are
         *         supported, returns an array of length 0.
         * @throws NullPointerException if {@code sequence} is {@code null}
         */
        public static int[] getMidiFileTypes(Sequence sequence) {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            IList<MidiFileWriter> providers = getMidiFileWriters();
            HashSet<Int32> allTypes = new HashSet<Int32>();

            // gather from all the providers

            for (int i = 0; i < providers.Count; i++) {
                MidiFileWriter writer = providers[i];
                int[] types = writer.getMidiFileTypes(sequence);
                for (int j = 0; j < types.Length; j++) {
                    allTypes.Add(types[j]);
                }
            }
            int[] resultTypes = new int[allTypes.Count];
            int index = 0;
            foreach (Int32 integer in allTypes) {
                resultTypes[index++] = integer;
            }
            return resultTypes;
        }

        /**
         * Indicates whether a MIDI file of the file type specified can be written
         * from the sequence indicated.
         *
         * @param  fileType the file type for which write capabilities are queried
         * @param  sequence the sequence for which file writing support is queried
         * @return {@code true} if the file type is supported for this sequence,
         *         otherwise {@code false}
         * @throws NullPointerException if {@code sequence} is {@code null}
         */
        public static bool isFileTypeSupported(int fileType, Sequence sequence) {
            if (sequence == null)
                throw new ArgumentNullException(nameof(sequence));

            IList<MidiFileWriter> providers = getMidiFileWriters();

            for (int i = 0; i < providers.Count; i++) {
                MidiFileWriter writer = providers[i];
                if (writer.isFileTypeSupported(fileType, sequence)) {
                    return true;
                }
            }
            return false;
        }

        /**
         * Writes a stream of bytes representing a file of the MIDI file type
         * indicated to the output stream provided.
         *
         * @param  in sequence containing MIDI data to be written to the file
         * @param  fileType the file type of the file to be written to the output
         *         stream
         * @param  out stream to which the file data should be written
         * @return the number of bytes written to the output stream
         * @throws IOException if an I/O exception occurs
         * @throws IllegalArgumentException if the file format is not supported by
         *         the system
         * @throws NullPointerException if {@code in} or {@code out} are
         *         {@code null}
         * @see #isFileTypeSupported(int, Sequence)
         * @see #getMidiFileTypes(Sequence)
         */
        public static int write(Sequence input, int fileType, Stream output) {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            IList<MidiFileWriter> providers = getMidiFileWriters();
            //$$fb 2002-04-17: Fix for 4635287: Standard MidiFileWriter cannot write empty Sequences
            int bytesWritten = -2;

            for (int i = 0; i < providers.Count; i++) {
                MidiFileWriter writer = providers[i];
                if (writer.isFileTypeSupported(fileType, input)) {

                    bytesWritten = writer.write(input, fileType, output);
                    break;
                }
            }
            if (bytesWritten == -2) {
                throw new ArgumentException("MIDI file type is not supported");
            }
            return bytesWritten;
        }

        /**
         * Writes a stream of bytes representing a file of the MIDI file type
         * indicated to the external file provided.
         *
         * @param  in sequence containing MIDI data to be written to the file
         * @param  type the file type of the file to be written to the output stream
         * @param  out external file to which the file data should be written
         * @return the number of bytes written to the file
         * @throws IOException if an I/O exception occurs
         * @throws IllegalArgumentException if the file type is not supported by the
         *         system
         * @throws NullPointerException if {@code in} or {@code out} are
         *         {@code null}
         * @see #isFileTypeSupported(int, Sequence)
         * @see #getMidiFileTypes(Sequence)
         */
        public static int write(Sequence input, int type, FileInfo output) {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (output == null)
                throw new ArgumentNullException(nameof(output));

            IList<MidiFileWriter> providers = getMidiFileWriters();
            //$$fb 2002-04-17: Fix for 4635287: Standard MidiFileWriter cannot write empty Sequences
            int bytesWritten = -2;

            for (int i = 0; i < providers.Count; i++) {
                MidiFileWriter writer = providers[i];
                if (writer.isFileTypeSupported(type, input)) {

                    bytesWritten = writer.write(input, type, output);
                    break;
                }
            }
            if (bytesWritten == -2) {
                throw new ArgumentException("MIDI file type is not supported");
            }
            return bytesWritten;
        }

        // HELPER METHODS

        /**
         * Obtains the list of MidiDeviceProviders installed on the system.
         *
         * @return the list of MidiDeviceProviders installed on the system
         */
        private static IList<MidiDeviceProvider> getMidiDeviceProviders() {
            //return (IList<MidiDeviceProvider>)getProviders(typeof(MidiDeviceProvider));
            IList providers = getProviders(typeof(MidiDeviceProvider));
            List<MidiDeviceProvider> result = new List<MidiDeviceProvider>();
            foreach (var provider in providers) {
                result.Add((MidiDeviceProvider)provider);
            }
            return result;
        }

        /**
         * Obtains the list of SoundbankReaders installed on the system.
         *
         * @return the list of SoundbankReaders installed on the system
         */
        private static IList<SoundbankReader> getSoundbankReaders() {
            //return (IList<SoundbankReader>)getProviders(typeof(SoundbankReader));
            IList providers = getProviders(typeof(SoundbankReader));
            List<SoundbankReader> result = new List<SoundbankReader>();
            foreach (var provider in providers) {
                result.Add((SoundbankReader)provider);
            }
            return result;
        }

        /**
         * Obtains the list of MidiFileWriters installed on the system.
         *
         * @return the list of MidiFileWriters installed on the system
         */
        private static IList<MidiFileWriter> getMidiFileWriters() {
            //return (IList<MidiFileWriter>)getProviders(typeof(MidiFileWriter));
            IList providers = getProviders(typeof(MidiFileWriter));
            List<MidiFileWriter> result = new List<MidiFileWriter>();
            foreach (var provider in providers) {
                result.Add((MidiFileWriter)provider);
            }
            return result;
        }

        /**
         * Obtains the list of MidiFileReaders installed on the system.
         *
         * @return the list of MidiFileReaders installed on the system
         */
        private static IList<MidiFileReader> getMidiFileReaders() {
            //return (IList<MidiFileReader>)getProviders(typeof(MidiFileReader));
            IList providers = getProviders(typeof(MidiFileReader));
            List<MidiFileReader> result = new List<MidiFileReader>();
            foreach (var provider in providers) {
                result.Add((MidiFileReader)provider);
            }
            return result;
        }

        /**
         * Attempts to locate and return a default MidiDevice of the specified type.
         * This method wraps {@link #getDefaultDevice}. It catches the
         * {@code IllegalArgumentException} thrown by {@code getDefaultDevice} and
         * instead throws a {@code MidiUnavailableException}, with the caught
         * exception chained.
         *
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @return default MidiDevice of the specified type
         * @throws MidiUnavailableException on failure
         */
        private static IMidiDevice getDefaultDeviceWrapper(Type deviceClass) {
            try {
                return getDefaultDevice(deviceClass);
            } catch (ArgumentException iae) {
                MidiUnavailableException mae = new MidiUnavailableException(iae);
                throw mae;
            }
        }

        /**
         * Attempts to locate and return a default MidiDevice of the specified type.
         *
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @return default MidiDevice of the specified type.
         * @throws IllegalArgumentException on failure
         */
        private static IMidiDevice getDefaultDevice(Type deviceClass) {
            IList<MidiDeviceProvider> providers = getMidiDeviceProviders();
            String providerClassName = JDK13Services.getDefaultProviderClassName(deviceClass);
            String instanceName = JDK13Services.getDefaultInstanceName(deviceClass);
            IMidiDevice device;

            if (providerClassName != null) {
                MidiDeviceProvider defaultProvider = getNamedProvider(providerClassName, providers);
                if (defaultProvider != null) {
                    if (instanceName != null) {
                        device = getNamedDevice(instanceName, defaultProvider, deviceClass);
                        if (device != null) {
                            return device;
                        }
                    }
                    device = getFirstDevice(defaultProvider, deviceClass);
                    if (device != null) {
                        return device;
                    }
                }
            }

            /*
             *  - Provider class not specified or cannot be found, or
             *  - provider class specified, and no appropriate device available, or
             *  - provider class and instance specified and instance cannot be found
             *    or is not appropriate
             */
            if (instanceName != null) {
                device = getNamedDevice(instanceName, providers, deviceClass);
                if (device != null) {
                    return device;
                }
            }

            /*
             * No defaults are specified, or if something is specified, everything
             * failed
             */
            device = getFirstDevice(providers, deviceClass);
            if (device != null) {
                return device;
            }
            throw new ArgumentException("Requested device not installed");
        }

        /**
         * Return a MidiDeviceProvider of a given class from the list of
         * MidiDeviceProviders.
         *
         * @param  providerClassName The class name of the provider to be returned
         * @param  providers The list of MidiDeviceProviders that is searched
         * @return A MidiDeviceProvider of the requested class, or null if none is
         *         found
         */
        private static MidiDeviceProvider getNamedProvider(String providerClassName, IList<MidiDeviceProvider> providers) {
            for (int i = 0; i < providers.Count; i++) {
                MidiDeviceProvider provider = providers[i];
                if (provider.GetType().FullName.Equals(providerClassName)) {
                    return provider;
                }
            }
            return null;
        }

        /**
         * Return a MidiDevice with a given name from a given MidiDeviceProvider.
         *
         * @param  deviceName The name of the MidiDevice to be returned
         * @param  provider The MidiDeviceProvider to check for MidiDevices
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @return A MidiDevice matching the requirements, or null if none is found
         */
        private static IMidiDevice getNamedDevice(String deviceName,
                             MidiDeviceProvider provider,
                             Type deviceClass) {
            IMidiDevice device;
            // try to get MIDI port
            device = getNamedDevice(deviceName, provider, deviceClass,
                         false, false);
            if (device != null) {
                return device;
            }

            if (deviceClass == typeof(IReceiver)) {
                // try to get Synthesizer
                device = getNamedDevice(deviceName, provider, deviceClass,
                             true, false);
                if (device != null) {
                    return device;
                }
            }

            return null;
        }

        /**
         * Return a MidiDevice with a given name from a given MidiDeviceProvider.
         *
         * @param  deviceName The name of the MidiDevice to be returned
         * @param  provider The MidiDeviceProvider to check for MidiDevices
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @param  allowSynthesizer if true, Synthesizers are considered
         *         appropriate. Otherwise only pure MidiDevices are considered
         *         appropriate (unless allowSequencer is true). This flag only has
         *         an effect for deviceClass Receiver and Transmitter. For other
         *         device classes (Sequencer and Synthesizer), this flag has no
         *         effect.
         * @param  allowSequencer if true, Sequencers are considered appropriate.
         *         Otherwise only pure MidiDevices are considered appropriate
         *         (unless allowSynthesizer is true). This flag only has an effect
         *         for deviceClass Receiver and Transmitter. For other device
         *         classes (Sequencer and Synthesizer), this flag has no effect.
         * @return A MidiDevice matching the requirements, or null if none is found
         */
        private static IMidiDevice getNamedDevice(String deviceName,
                             MidiDeviceProvider provider,
                             Type deviceClass,
                             bool allowSynthesizer,
                             bool allowSequencer) {
            MidiDevice.Info[] infos = provider.getDeviceInfo();
            for (int i = 0; i < infos.Length; i++) {
                if (infos[i].getName().Equals(deviceName)) {
                    IMidiDevice device = provider.getDevice(infos[i]);
                    if (isAppropriateDevice(device, deviceClass,
                                allowSynthesizer, allowSequencer)) {
                        return device;
                    }
                }
            }
            return null;
        }

        /**
         * Return a MidiDevice with a given name from a list of MidiDeviceProviders.
         *
         * @param  deviceName The name of the MidiDevice to be returned
         * @param  providers The List of MidiDeviceProviders to check for
         *         MidiDevices
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @return A Mixer matching the requirements, or null if none is found
         */
        private static IMidiDevice getNamedDevice(String deviceName,
                             IList<MidiDeviceProvider> providers,
                             Type deviceClass) {
            IMidiDevice device;
            // try to get MIDI port
            device = getNamedDevice(deviceName, providers, deviceClass,
                         false, false);
            if (device != null) {
                return device;
            }

            if (deviceClass == typeof(IReceiver)) {
                // try to get Synthesizer
                device = getNamedDevice(deviceName, providers, deviceClass,
                             true, false);
                if (device != null) {
                    return device;
                }
            }
            return null;
        }

        /**
         * Return a MidiDevice with a given name from a list of MidiDeviceProviders.
         *
         * @param  deviceName The name of the MidiDevice to be returned
         * @param  providers The List of MidiDeviceProviders to check for
         *         MidiDevices
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @param  allowSynthesizer if true, Synthesizers are considered
         *         appropriate. Otherwise only pure MidiDevices are considered
         *         appropriate (unless allowSequencer is true). This flag only has
         *         an effect for deviceClass Receiver and Transmitter. For other
         *         device classes (Sequencer and Synthesizer), this flag has no
         *         effect.
         * @param  allowSequencer if true, Sequencers are considered appropriate.
         *         Otherwise only pure MidiDevices are considered appropriate
         *         (unless allowSynthesizer is true). This flag only has an effect
         *         for deviceClass Receiver and Transmitter. For other device
         *         classes (Sequencer and Synthesizer), this flag has no effect.
         * @return A Mixer matching the requirements, or null if none is found
         */
        private static IMidiDevice getNamedDevice(String deviceName,
                             IList<MidiDeviceProvider> providers,
                             Type deviceClass,
                             bool allowSynthesizer,
                             bool allowSequencer) {
            for (int i = 0; i < providers.Count; i++) {
                MidiDeviceProvider provider = providers[i];
                IMidiDevice device = getNamedDevice(deviceName, provider,
                                   deviceClass,
                                   allowSynthesizer,
                                   allowSequencer);
                if (device != null) {
                    return device;
                }
            }
            return null;
        }

        /**
         * From a given MidiDeviceProvider, return the first appropriate device.
         *
         * @param  provider The MidiDeviceProvider to check for MidiDevices
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @return A MidiDevice is considered appropriate, or null if no appropriate
         *         device is found
         */
        private static IMidiDevice getFirstDevice(MidiDeviceProvider provider,
                             Type deviceClass) {
            IMidiDevice device;
            // try to get MIDI port
            device = getFirstDevice(provider, deviceClass,
                        false, false);
            if (device != null) {
                return device;
            }

            if (deviceClass == typeof(IReceiver)) {
                // try to get Synthesizer
                device = getFirstDevice(provider, deviceClass,
                            true, false);
                if (device != null) {
                    return device;
                }
            }

            return null;
        }

        /**
         * From a given MidiDeviceProvider, return the first appropriate device.
         *
         * @param  provider The MidiDeviceProvider to check for MidiDevices
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @param  allowSynthesizer if true, Synthesizers are considered
         *         appropriate. Otherwise only pure MidiDevices are considered
         *         appropriate (unless allowSequencer is true). This flag only has
         *         an effect for deviceClass Receiver and Transmitter. For other
         *         device classes (Sequencer and Synthesizer), this flag has no
         *         effect.
         * @param  allowSequencer if true, Sequencers are considered appropriate.
         *         Otherwise only pure MidiDevices are considered appropriate
         *         (unless allowSynthesizer is true). This flag only has an effect
         *         for deviceClass Receiver and Transmitter. For other device
         *         classes (Sequencer and Synthesizer), this flag has no effect.
         * @return A MidiDevice is considered appropriate, or null if no appropriate
         *         device is found
         */
        private static IMidiDevice getFirstDevice(MidiDeviceProvider provider,
                             Type deviceClass,
                             bool allowSynthesizer,
                             bool allowSequencer) {
            MidiDevice.Info[] infos = provider.getDeviceInfo();
            for (int j = 0; j < infos.Length; j++) {
                IMidiDevice device = provider.getDevice(infos[j]);
                if (isAppropriateDevice(device, deviceClass,
                            allowSynthesizer, allowSequencer)) {
                    return device;
                }
            }
            return null;
        }

        /**
         * From a List of MidiDeviceProviders, return the first appropriate
         * MidiDevice.
         *
         * @param  providers The List of MidiDeviceProviders to search
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @return A MidiDevice that is considered appropriate, or null if none is
         *         found
         */
        private static IMidiDevice getFirstDevice(IList<MidiDeviceProvider> providers,
                             Type deviceClass) {
            IMidiDevice device;
            // try to get MIDI port
            device = getFirstDevice(providers, deviceClass,
                        false, false);
            if (device != null) {
                return device;
            }

            if (deviceClass == typeof(IReceiver)) {
                // try to get Synthesizer
                device = getFirstDevice(providers, deviceClass,
                            true, false);
                if (device != null) {
                    return device;
                }
            }

            return null;
        }

        /**
         * From a List of MidiDeviceProviders, return the first appropriate
         * MidiDevice.
         *
         * @param  providers The List of MidiDeviceProviders to search
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @param  allowSynthesizer if true, Synthesizers are considered
         *         appropriate. Otherwise only pure MidiDevices are considered
         *         appropriate (unless allowSequencer is true). This flag only has
         *         an effect for deviceClass Receiver and Transmitter. For other
         *         device classes (Sequencer and Synthesizer), this flag has no
         *         effect.
         * @param  allowSequencer if true, Sequencers are considered appropriate.
         *         Otherwise only pure MidiDevices are considered appropriate
         *         (unless allowSynthesizer is true). This flag only has an effect
         *         for deviceClass Receiver and Transmitter. For other device
         *         classes (Sequencer and Synthesizer), this flag has no effect.
         * @return A MidiDevice that is considered appropriate, or null if none is
         *         found
         */
        private static IMidiDevice getFirstDevice(IList<MidiDeviceProvider> providers,
                             Type deviceClass,
                             bool allowSynthesizer,
                             bool allowSequencer) {
            for (int i = 0; i < providers.Count; i++) {
                MidiDeviceProvider provider = providers[i];
                IMidiDevice device = getFirstDevice(provider, deviceClass,
                                   allowSynthesizer,
                                   allowSequencer);
                if (device != null) {
                    return device;
                }
            }
            return null;
        }

        /**
         * Checks if a MidiDevice is appropriate. If deviceClass is Synthesizer or
         * Sequencer, a device implementing the respective interface is considered
         * appropriate. If deviceClass is Receiver or Transmitter, a device is
         * considered appropriate if it implements neither Synthesizer nor
         * Transmitter, and if it can provide at least one Receiver or Transmitter,
         * respectively.
         *
         * @param  device the MidiDevice to test
         * @param  deviceClass The requested device type, one of Synthesizer.class,
         *         Sequencer.class, Receiver.class or Transmitter.class
         * @param  allowSynthesizer if true, Synthesizers are considered
         *         appropriate. Otherwise only pure MidiDevices are considered
         *         appropriate (unless allowSequencer is true). This flag only has
         *         an effect for deviceClass Receiver and Transmitter. For other
         *         device classes (Sequencer and Synthesizer), this flag has no
         *         effect.
         * @param  allowSequencer if true, Sequencers are considered appropriate.
         *         Otherwise only pure MidiDevices are considered appropriate
         *         (unless allowSynthesizer is true). This flag only has an effect
         *         for deviceClass Receiver and Transmitter. For other device
         *         classes (Sequencer and Synthesizer), this flag has no effect.
         * @return true if the device is considered appropriate according to the
         *         rules given above, false otherwise
         */
        private static bool isAppropriateDevice(IMidiDevice device,
                               Type deviceClass,
                               bool allowSynthesizer,
                               bool allowSequencer) {
            if (deviceClass.IsInstanceOfType(device)) {
                // This clause is for deviceClass being either Synthesizer
                // or Sequencer.
                return true;
            } else {
                // Now the case that deviceClass is Transmitter or
                // Receiver. If neither allowSynthesizer nor allowSequencer is
                // true, we require device instances to be
                // neither Synthesizer nor Sequencer, since we only want
                // devices representing MIDI ports.
                // Otherwise, the respective type is accepted, too
                if ((!(device is ISequencer) &&
                  !(device is ISynthesizer)) ||
                 ((device is ISequencer) && allowSequencer) ||
                 ((device is ISynthesizer) && allowSynthesizer)) {
                    // And of course, the device has to be able to provide
                    // Receivers or Transmitters.
                    if ((deviceClass == typeof(IReceiver) &&
                         device.getMaxReceivers() != 0) ||
                        (deviceClass == typeof(ITransmitter) &&
                         device.getMaxTransmitters() != 0)) {
                        return true;
                    }
                }
            }
            return false;
        }

        /**
         * Obtains the set of services currently installed on the system using the
         * SPI mechanism in 1.3.
         *
         * @param  providerClass The type of providers requested. This should be one
         *         of AudioFileReader.class, AudioFileWriter.class,
         *         FormatConversionProvider.class, MixerProvider.class,
         *         MidiDeviceProvider.class, MidiFileReader.class,
         *         MidiFileWriter.class or SoundbankReader.class.
         * @return a List of instances of providers for the requested service. If no
         *         providers are available, a List of length 0 will be returned.
         */
        private static IList getProviders(Type providerClass) {
            return JDK13Services.getProviders(providerClass);
        }
    }
}
