/*
 * Copyright (c) 2007, 2024, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.BufferedReader;
//import java.io.File;
//import java.io.IOException;
//import java.io.InputStream;
//import java.io.InputStreamReader;
//import java.net.URL;
//import java.net.URLClassLoader;
//import java.util.ArrayList;
//import java.util.Objects;

//import javax.sound.midi.InvalidMidiDataException;
//import javax.sound.midi.Soundbank;
//import javax.sound.midi.spi.SoundbankReader;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using SystemX.Sound.Midi;
using SystemX.Addon;

namespace SystemX.Media.Sound {

    /**
     * JarSoundbankReader is used to read soundbank object from jar files.
     *
     * @author Karl Helgason
     */
    public sealed class JARSoundbankReader : SoundbankReader {

        /*
         * Value of the system property that enables the Jar soundbank loading
         * {@code true} if jar sound bank is allowed to be loaded default is
         * {@code false}
        */
        private const String JAR_SOUNDBANK_ENABLED = "jdk.sound.jarsoundbank";

        private static bool isZIP(Uri url) {
            bool ok = false;
            try {
                using (Stream stream = UrlHelper.openStream(url)) {
                    byte[] buff = new byte[4];
                    ok = stream.Read(buff, 0, 4) == 4;
                    if (ok) {
                        ok = (buff[0] == 0x50
                            && buff[1] == 0x4b
                            && buff[2] == 0x03
                            && buff[3] == 0x04);
                    }
                }
            } catch (IOException) {
            }
            return ok;
        }

        public override ISoundbank getSoundbank(Uri url) {
            if (url == null)
                throw new ArgumentNullException(nameof(url));
            if (!url.IsFile) {
                return null;
            }

            Assembly acl;
            Stream stream;
            List<ISoundbank> soundbanks = new List<ISoundbank>();
            try {
                acl = Assembly.LoadFile(url.LocalPath);
                String assemblyName = Path.GetFileNameWithoutExtension(url.LocalPath);
                stream = acl.GetManifestResourceStream(assemblyName + ".META-INF.services.SystemX.Sound.Midi.ISoundbank"); //@ ???
            } catch (Exception) {
                return null;
            }
            if (stream == null) {
                return null;
            }
            using (stream) {
                StreamReader r = new StreamReader(stream);
                String line = r.ReadLine();
                while (line != null) {
                    if (!line.StartsWith("#", StringComparison.Ordinal)) {
                        try {
                            //@ different to Java implementation
                            String typeName = line.Trim();
                            Type class1 = acl.GetType(typeName, false);
                            if (class1 != null) {
                                Object obj = Activator.CreateInstance(class1);
                                if (obj is ISoundbank) {
                                    soundbanks.Add((ISoundbank)obj);
                                }
                            }
                        } catch (TypeLoadException) {//@ different to Java implementation
                        } catch (TargetInvocationException) {
                        }
                    }
                    line = r.ReadLine();
                }
            }
            if (soundbanks.Count == 0) {
                return null;
            }
            if (soundbanks.Count == 1) {
                return soundbanks[0];
            }
            SimpleSoundbank sbk = new SimpleSoundbank();
            foreach (ISoundbank soundbank in soundbanks) {
                sbk.addAllInstruments(soundbank);
            }
            return sbk;
        }

        public override ISoundbank getSoundbank(Stream stream) {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            return null;
        }

        public override ISoundbank getSoundbank(FileInfo file) {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            return getSoundbank(new Uri(file.FullName));
        }
    }
}
