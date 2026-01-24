#undef NoNative
//#define NoNative
/*
 * Copyright (c) 1999, 2026, Oracle and/or its affiliates. All rights reserved.
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

//@Todo

//package com.sun.media.sound;    

//import java.util.StringTokenizer;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Reflection;
using SystemX.Addon;
using Windows.Win32.Foundation;
using Windows.Win32;
using System.Runtime.CompilerServices;

namespace SystemX.Media.Sound
/**
 * Audio configuration class for exposing attributes specific to the platform or system.
 *
 * @author Kara Kytle
 * @author Florian Bomers
 */
{
    internal sealed partial class Platform {
        // native library we need to load
        private const String libName = "CSound.dll";

        private static bool isNativeLibLoaded;

        static Platform() {
            //SetEnvironment();
            loadLibraries();
        }

        /**
         * Private constructor.
         */
        private Platform() {
        }

        private static void SetEnvironment() {
            String path = Service.GetClassPathDirectory(); //Directory of the native Sound dll (CSound)
            if (path != null) {
                try {
                    String pathEnv = Environment.GetEnvironmentVariable("Path");
                    pathEnv += ";" + path;
                    Environment.SetEnvironmentVariable("Path", pathEnv);
                } catch (SecurityException) {

                }
            }
        }

        /**
         * Dummy method for forcing initialization.
         */
        internal static void initialize() {
        }

        /**
         * Determine whether the system is big-endian.
         */
        internal static bool isBigEndian() {
            return !BitConverter.IsLittleEndian;
        }

        private static GCHandle soundDllHandle;

        /**
         * Load the native library or libraries.
         */
        private static void loadLibraries() {
            // load the native library
            isNativeLibLoaded = true;
            try {
                string path = Service.GetClassPathDirectory();
                string fileName = null;
                if (path != null) {
                    fileName = Path.Combine(path, libName);
                }
                if (path == null || !File.Exists(fileName)) {
                    Assembly soundAssembly = Assembly.GetExecutingAssembly();
                    path = Path.GetDirectoryName(soundAssembly.Location);
                    if (IntPtr.Size == 8)
                        path = Path.Combine(path, "x64");
                    else
                        path = Path.Combine(path, "x86");
                    fileName = Path.Combine(path, libName);
                }
                soundDllHandle = GCHandle.Alloc(SafeLibraryHandle.LoadLibraryEx(fileName, 0));
            }
            //FileIOPermission perm = new FileIOPermission(FileIOPermissionAccess.Read, fileName);
            //perm.Assert();
            //PrivilegedAction action = delegate {
            //        System.loadLibrary(libName);
            //        return null;
            //    };
            //AccessController.doPrivileged(action);
            //FileIOPermission.RevertAssert();

                catch (Exception t) {
                if (Printer.err) Printer.Err("Couldn't load library " + libName + ": " + t.ToString());
                isNativeLibLoaded = false;
            }
        }

        public static bool isMidiIOEnabled() {
            return isNativeLibLoaded;
        }

        public static bool isPortsEnabled() {
            return isNativeLibLoaded;
        }

        public static bool isDirectAudioEnabled() {
            return isNativeLibLoaded;
        }

#pragma warning disable CA1416

        private partial class NativeMethods {
            public static unsafe bool SetDllDirectory(String lpPathName) {
                BOOL result;
                fixed (char* pChar = lpPathName)
                    result = PInvoke.SetDllDirectory(new PCWSTR(pChar));
                return result;
            }

            public static unsafe string GetDllDirectory() {
                uint result;
                uint nBufferLength = 260;
                char* pChar = stackalloc char[260];
                PWSTR pwstr = new PWSTR(pChar);
                result = PInvoke.GetDllDirectory(nBufferLength, pwstr);
                return pwstr.ToString();
            }
        }
    }
}
