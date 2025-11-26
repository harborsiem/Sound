/*
 * Copyright (c) 1999, 2024, Oracle and/or its affiliates. All rights reserved.
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

//import java.security.AccessController;
//import java.security.PrivilegedAction;
//import java.util.ArrayList;
//import java.util.Collections;
//import java.util.List;
//import java.util.Properties;

//import javax.sound.midi.Receiver;
//import javax.sound.midi.Sequencer;
//import javax.sound.midi.Synthesizer;
//import javax.sound.midi.Transmitter;
//import javax.sound.midi.spi.MidiDeviceProvider;
//import javax.sound.midi.spi.MidiFileReader;
//import javax.sound.midi.spi.MidiFileWriter;
//import javax.sound.midi.spi.SoundbankReader;
//import javax.sound.sampled.Clip;
//import javax.sound.sampled.Port;
//import javax.sound.sampled.SourceDataLine;
//import javax.sound.sampled.TargetDataLine;
//import javax.sound.sampled.spi.AudioFileReader;
//import javax.sound.sampled.spi.AudioFileWriter;
//import javax.sound.sampled.spi.FormatConversionProvider;
//import javax.sound.sampled.spi.MixerProvider;

using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Runtime.CompilerServices;
using SystemX.Addon;
using SystemX.Sound.Midi;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * JDK13Services uses the Service class in JDK 1.3 to discover a list of service
 * providers installed in the system.
 * <p>
 * This class is public because it is called from javax.sound.midi.MidiSystem
 * and javax.sound.sampled.AudioSystem. The alternative would be to make
 * JSSecurityManager public, which is considered worse.
 *
 * @author Matthias Pfisterer
 */
    public sealed class JDK13Services {

        /**
         * Properties loaded from the properties file for default provider
         * properties.
        */
        private static IDictionary<String, String> properties;


        /**
         * Private, no-args constructor to ensure against instantiation.
         */
        private JDK13Services() {
        }

        /**
         * Obtains a List containing installed instances of the providers for the
         * requested service. The returned List is immutable.
         *
         * @param serviceClass The type of providers requested. This should be one
         *                     of AudioFileReader.class, AudioFileWriter.class,
         *                     FormatConversionProvider.class, MixerProvider.class,
         *                     MidiDeviceProvider.class, MidiFileReader.class,
         *                     MidiFileWriter.class or SoundbankReader.class.
         *
         * @return A List of providers of the requested type. This List is
         *         immutable.
         */
        public static IList getProviders(Type serviceClass) {
            IList providers;
            if (!typeof(MixerProvider).Equals(serviceClass)
                    && !typeof(FormatConversionProvider).Equals(serviceClass)
                    && !typeof(AudioFileReader).Equals(serviceClass)
                    && !typeof(AudioFileWriter).Equals(serviceClass)
                    && !typeof(MidiDeviceProvider).Equals(serviceClass)
                    && !typeof(SoundbankReader).Equals(serviceClass)
                    && !typeof(MidiFileWriter).Equals(serviceClass)
                    && !typeof(MidiFileReader).Equals(serviceClass)) {
                providers = Array.Empty<ArrayList>();
            } else {
                providers = JSSecurityManager.getProviders(serviceClass);
            }
            return ArrayList.ReadOnly(providers);
        }

        /** Obtain the provider class name part of a default provider property.
        @param typeClass The type of the default provider property. This
        should be one of Receiver.class, Transmitter.class, Sequencer.class,
        Synthesizer.class, SourceDataLine.class, TargetDataLine.class,
        Clip.class or Port.class.
        @return The value of the provider class name part of the property
        (the part before the hash sign), if available. If the property is
        not set or the value has no provider class name part, null is returned.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static String getDefaultProviderClassName(Type typeClass) {
            String value = null;
            String defaultProviderSpec = getDefaultProvider(typeClass);
            if (defaultProviderSpec != null) {
                int hashpos = defaultProviderSpec.IndexOf('#');
                if (hashpos == 0) {
                    // instance name only; leave value as null
                } else if (hashpos > 0) {
                    value = defaultProviderSpec.Substring(0, hashpos);
                } else {
                    value = defaultProviderSpec;
                }
            }
            return value;
        }

        /** Obtain the instance name part of a default provider property.
        @param typeClass The type of the default provider property. This
        should be one of Receiver.class, Transmitter.class, Sequencer.class,
        Synthesizer.class, SourceDataLine.class, TargetDataLine.class,
        Clip.class or Port.class.
        @return The value of the instance name part of the property (the
        part after the hash sign), if available. If the property is not set
        or the value has no instance name part, null is returned.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static String getDefaultInstanceName(Type typeClass) {
            String value = null;
            String defaultProviderSpec = getDefaultProvider(typeClass);
            if (defaultProviderSpec != null) {
                int hashpos = defaultProviderSpec.IndexOf('#');
                if (hashpos >= 0 && hashpos < defaultProviderSpec.Length - 1) {
                    value = defaultProviderSpec.Substring(hashpos + 1);
                }
            }
            return value;
        }

        /** Obtain the value of a default provider property.
        @param typeClass The type of the default provider property. This
        should be one of Receiver.class, Transmitter.class, Sequencer.class,
        Synthesizer.class, SourceDataLine.class, TargetDataLine.class,
        Clip.class or Port.class.
        @return The complete value of the property, if available.
        If the property is not set, null is returned.
         */
        [MethodImpl(MethodImplOptions.Synchronized)]
        private static String getDefaultProvider(Type typeClass) {
            if (!typeof(ISourceDataLine).Equals(typeClass)
                    && !typeof(ITargetDataLine).Equals(typeClass)
                    && !typeof(IClip).Equals(typeClass)
                    && !typeof(IPort).Equals(typeClass)
                    && !typeof(IReceiver).Equals(typeClass)
                    && !typeof(ITransmitter).Equals(typeClass)
                    && !typeof(ISynthesizer).Equals(typeClass)
                    && !typeof(ISequencer).Equals(typeClass)) {
                return null;
            }
            String name = typeClass.FullName;
            //EnvironmentPermission perm = new EnvironmentPermission(PermissionState.Unrestricted);
            //perm.Assert();
            String value = getSystemProperty(name);
            //EnvironmentPermission.RevertAssert();
            if (value == null) {
                if (getProperties().ContainsKey(name)) {
                    value = getProperties()[name];
                }
            }
            if (String.IsNullOrEmpty(value)) {
                value = null;
            }
            return value;
        }

        private static String getSystemProperty(String propertyName) { //a@ added
            switch (propertyName) {
                case "java.home": return Service.GetHome();
                case "java.class.path": return Service.GetClassPath();
                default: break;
            }
            return null;
        }

        /** Obtain a properties bundle containing property values from the
        properties file. If the properties file could not be loaded,
        the properties bundle is empty.
        */
        [MethodImpl(MethodImplOptions.Synchronized)]
        private static IDictionary<String, String> getProperties() {
            if (properties == null) {
                properties = new Dictionary<String, String>();
                JSSecurityManager.loadProperties(properties);
            }
            return properties;
        }
    }
}
