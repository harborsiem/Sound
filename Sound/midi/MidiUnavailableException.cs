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

//package javax.sound.midi;       

//import java.io.Serial;

using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace SystemX.Sound.Midi {
/**
 * A {@code MidiUnavailableException} is thrown when a requested MIDI component
 * cannot be opened or created because it is unavailable. This often occurs when
 * a device is in use by another application. More generally, it can occur when
 * there is a finite number of a certain kind of resource that can be used for
 * some purpose, and all of them are already in use (perhaps all by this
 * application). For an example of the latter case, see the
 * {@link Transmitter#setReceiver(Receiver) setReceiver} method of
 * {@code Transmitter}.
 *
 * @author Kara Kytle
 */
    [Serializable]
    public sealed class MidiUnavailableException : Exception {

        /**
         * Use serialVersionUID from JDK 1.3 for interoperability.
         */
        private const long serialVersionUID = 6093809578628944323L;

        /**
         * Constructs a {@code MidiUnavailableException} that has {@code null} as
         * its error detail message.
         */
        public MidiUnavailableException()

            : base() {
        }

        /**
         * Constructs a {@code MidiUnavailableException} with the specified detail
         * message.
         *
         * @param  message the string to display as an error detail message
         */
        public MidiUnavailableException(String message)

            : base(message) {
        }

        public MidiUnavailableException(Exception innerException)

            : base(innerException.Message, innerException) {
        }

        public MidiUnavailableException(String message, Exception innerException)
            : base(message, innerException) {
        }
    }
}
