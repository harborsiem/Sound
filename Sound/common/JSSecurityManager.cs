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

//import java.io.BufferedInputStream;
//import java.io.InputStream;
//import java.io.File;
//import java.io.FileInputStream;

//import java.util.ArrayList;
//import java.util.Iterator;
//import java.util.List;
//import java.util.Properties;
//import java.util.ServiceLoader;


using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Threading;
using System.Runtime.CompilerServices;
using SystemX.Addon;

namespace SystemX.Media.Sound {
    /**
    * Historically this class managed ensuring privileges to access resources
    * it is still used to get those resources but no longer does checks.
    *
    * @author Matthias Pfisterer
    */
    internal sealed class JSSecurityManager {

        /**
         * Filename of the properties file for default provider properties. This
         * file is searched in the subdirectory "lib" of the JRE directory (this
         * behaviour is hardcoded).
        */
        private const String PROPERTIES_FILENAME = "sound.config";

        /** Prevent instantiation.
         */
        private JSSecurityManager() {
        }

        delegate T PrivilegedAction<T>();

        /**
         * Load properties from a file.
         * <p>
         * This method tries to load properties from the filename give into the
         * passed properties object. If the file cannot be found or something else
         * goes wrong, the method silently fails.
         * <p>
         * If the file referenced in "javax.sound.config.file" property exists and
         * the user has an access to it, then it will be loaded, otherwise default
         * configuration file "JAVA_HOME/conf/sound.properties" will be loaded.
         *
         * @param  properties the properties bundle to store the values of the
         *         properties file
         */
        internal static void loadProperties(IDictionary<String, String> properties) {
            //new FileIOPermission(PermissionState.Unrestricted).Assert();
            //CodeAccessPermission.RevertAssert();
            String customFile = PROPERTIES_FILENAME;
            if (customFile != null) {
                if (loadPropertiesImpl(properties, customFile)) {
                    return;
                }
            }
            try {
                // invoke the privileged action using 1.2 security
                //PrivilegedAction<Void> action = delegate {
                //        loadPropertiesImpl(properties, filename);
                //        return null;
                //    };
                //AccessController.doPrivileged(action);
            } catch (Exception) {
                // try without using JDK 1.2 security
                loadPropertiesImpl(properties, PROPERTIES_FILENAME);
            }
        }

        private static bool loadPropertiesImpl(IDictionary<String, String> properties,
                           String filename) {
            if (filename == "sound.config") {
                IDictionary<String, String> tmp = Service.GetDefaultClasses();
                IEnumerator<KeyValuePair<String, String>> it = tmp.GetEnumerator();
                while (it.MoveNext()) {
                    properties.Add(it.Current.Key, it.Current.Value);
                }
            }
            return true;
            //String fname = getSystemProperty("java.home");
            //try {
            //    if (fname == null) {
            //        throw new ArgumentException("Can't find java.home ??");
            //    }
            //FileInfo f = new FileInfo(Path.Combine(fname, "lib"));
            //f = new FileInfo(Path.Combine(f.FullName, filename));
            //fname = f.FullName;
            //Stream input = new FileStream(fname, FileMode.Open, FileAccess.Read);
            ////BufferedStream bin = new BufferedStream(input);
            //try {
            //    properties.load(input);
            //}
            //finally {
            //    if (input != null) {
            //        input.Close();
            //    }
            //}
            //}
            //catch (ArgumentException t) {
            //    if (Printer.trace) {
            //        Console.Error.WriteLine("Could not load properties file \"" + fname + "\"");
            //        printStackTrace(t);
            //    }
            //}
        }

        /** Create a Thread in the current ThreadGroup.
         */
        internal static Thread createThread(ThreadStart run,
                       String threadName,
                       bool isDaemon, ThreadPriority priority,
                       bool doStart) {
            Thread thread = new Thread(run);
            thread.Name = threadName;
            thread.IsBackground = isDaemon;
            //EventDispatcher, DirectAudioDevice => isDaemon = true
            thread.TrySetApartmentState(ApartmentState.STA); //@ ???
            if (priority >= 0) {
                thread.Priority = (ThreadPriority)(priority);
            }
            if (doStart) {
                thread.Start();
            }
            return thread;
        }

        //@Todo: ServiceLoader Java 8
        [MethodImpl(MethodImplOptions.Synchronized)]
        internal static IList getProviders(Type providerClass) {
            //new FileIOPermission(PermissionState.Unrestricted).Assert();
            //CodeAccessPermission.RevertAssert();
            //for providers see addon\Service.cs

            ArrayList p = new ArrayList();
            // Service.providers(Class) just creates "lazy" iterator instance,
            // so it doesn't require do be called from privileged section

            IEnumerator ps = Service.GetProviders(providerClass);

            // the iterator's hasNext() method looks through classpath for
            // the provider class names, so it requires read permissions
            //PrivilegedAction<Boolean> hasNextAction = new PrivilegedAction<Boolean>() {
            //    public Boolean run() {
            //        return ps.MoveNext();
            //    }
            //};

            //while (AccessController.doPrivileged(hasNextAction)) {
            try {
                while (ps.MoveNext()) {
                    // the iterator's next() method creates instances of the
                    // providers and it should be called in the current security
                    // context
                    Object provider = ps.Current;
                    if (providerClass.IsInstanceOfType(provider)) {
                        // $$mp 2003-08-22
                        // Always adding at the beginning reverses the
                        // order of the providers. So we no longer have
                        // to do this in AudioSystem and MidiSystem.
                        p.Insert(0, provider);
                    }
                }
            } catch (Exception t) {
                //$$fb 2002-11-07: do not fail on SPI not found
                if (Printer.err) Printer.printStackTrace(t);
            }
            return p;
        }
    }
}
