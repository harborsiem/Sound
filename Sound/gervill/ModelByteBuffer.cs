/*
 * Copyright (c) 2007, 2018, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.ByteArrayInputStream;
//import java.io.DataInputStream;
//import java.io.File;
//import java.io.IOException;
//import java.io.InputStream;
//import java.io.OutputStream;
//import java.io.RandomAccessFile;
//import java.util.Collection;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;

namespace SystemX.Media.Sound {
/**
 * This class is a pointer to a binary array either in memory or on disk.
 *
 * @author Karl Helgason
 */
    public sealed class ModelByteBuffer {

        private ModelByteBuffer root; //a@ = this;
        private FileInfo file;
        private long fileoffset;
        private byte[] buffer;
        private long offset;
        private readonly long len;

        private class RandomFileInputStream : InputStream {

            private readonly FileStream raf;
            private long left;
            private long _mark = 0;
            private long markleft = 0;

            internal RandomFileInputStream(ModelByteBuffer caller) {
                raf = new FileStream(caller.root.file.FullName, FileMode.Open, FileAccess.Read);
                raf.Seek(caller.root.fileoffset + caller.arrayOffset(), SeekOrigin.Begin);
                left = caller.capacity();
            }

            public override void SetLength(long value) {
                raf.SetLength(value);
            }

            public override long Seek(long offset, SeekOrigin origin) {
                return raf.Seek(offset, origin);
            }

            public override long Position {
                get { return raf.Position; }
                set { raf.Position = value; }
            }

            public override long Length {
                get { return raf.Length; }
            }

            public override bool CanSeek {
                get { return true; }
            }

            public override int available() {
                if (left > Int32.MaxValue)
                    return Int32.MaxValue;
                return (int)left;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void mark(int readlimit) {
                try {
                    _mark = raf.Position; //getFilePointer()
                    markleft = left;
                } catch (IOException) {
                    //Printer.printStackTrace(e);
                }
            }

            public override bool markSupported() {
                return true;
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void reset() {
                raf.Seek(_mark, SeekOrigin.Begin);
                left = markleft;
            }

            public override long skip(long n) {
                if (n < 0)
                    return 0;
                if (n > left)
                    n = left;
                long p = raf.Position;
                raf.Seek(p + n, SeekOrigin.Begin);
                left -= n;
                return n;
            }

            public override int Read(byte[] b, int off, int len) {
                if (len > left)
                    len = (int)left;
                if (left == 0)
                    return -1;
                len = raf.Read(b, off, len);
                if (len == 0)
                    return 0;
                if (len == -1)
                    return -1;
                left -= len;
                return len;
            }

            public override int Read(byte[] b) {
                int len = b.Length;
                if (len > left)
                    len = (int)left;
                if (left == 0)
                    return -1;
                len = raf.Read(b, 0, len);
                if (len == 0)
                    return 0;
                if (len == -1)
                    return -1;
                left -= len;
                return len;
            }

            public override int ReadByte() {
                if (left == 0)
                    return -1;
                int b = raf.ReadByte();
                if (b == -1)
                    return -1;
                left--;
                return b;
            }

            public override void Close() {
                raf.Close();
            }
        }

        private ModelByteBuffer(ModelByteBuffer parent,
                long beginIndex, long endIndex, bool independent) {
            this.root = parent.root;
            this.offset = 0;
            long parent_len = parent.len;
            if (beginIndex < 0)
                beginIndex = 0;
            if (beginIndex > parent_len)
                beginIndex = parent_len;
            if (endIndex < 0)
                endIndex = 0;
            if (endIndex > parent_len)
                endIndex = parent_len;
            if (beginIndex > endIndex)
                beginIndex = endIndex;
            offset = beginIndex;
            len = endIndex - beginIndex;
            if (independent) {
                buffer = root.buffer;
                if (root.file != null) {
                    file = root.file;
                    fileoffset = root.fileoffset + arrayOffset();
                    offset = 0;
                } else
                    offset = arrayOffset();
                root = this;
            }
        }

        public ModelByteBuffer(byte[] buffer) {
            this.root = this;
            this.buffer = buffer;
            this.offset = 0;
            this.len = buffer.Length;
        }

        public ModelByteBuffer(byte[] buffer, int offset, int len) {
            this.root = this;
            this.buffer = buffer;
            this.offset = offset;
            this.len = len;
        }

        public ModelByteBuffer(FileInfo file) {
            this.root = this;
            this.file = file;
            this.fileoffset = 0;
            this.len = file.Length;
        }

        public ModelByteBuffer(FileInfo file, long offset, long len) {
            this.root = this;
            this.file = file;
            this.fileoffset = offset;
            this.len = len;
        }

        public void writeTo(Stream output) {
            if (root.file != null && root.buffer == null) {
                InputStream istream = null;
                using (Stream stream = getInputStream()) {
                    istream = stream as InputStream;
                    if (istream == null) {
                        istream = new InputStreamImpl(stream);
                    }
                    //byte[] buff = new byte[1024];
                    //int ret;
                    //while ((ret = istream.Read(buff)) > 0) {
                    //    output.Write(buff, 0, ret);
                    //}
                    istream.transferTo(output);
                }
            } else
                output.Write(array(), (int)arrayOffset(), (int)capacity());
        }

        public Stream getInputStream() {
            if (root.file != null && root.buffer == null) {
                try {
                    return new RandomFileInputStream(this);
                } catch (IOException) {
                    //Printer.printStackTrace(e);
                    return null;
                }
            }
            return new MemoryStream(array(),
                    (int)arrayOffset(), (int)capacity());
        }

        public ModelByteBuffer subbuffer(long beginIndex) {
            return subbuffer(beginIndex, capacity());
        }

        public ModelByteBuffer subbuffer(long beginIndex, long endIndex) {
            return subbuffer(beginIndex, endIndex, false);
        }

        public ModelByteBuffer subbuffer(long beginIndex, long endIndex,
                bool independent) {
            return new ModelByteBuffer(this, beginIndex, endIndex, independent);
        }

        public byte[] array() {
            return root.buffer;
        }

        public long arrayOffset() {
            if (root != this)
                return root.arrayOffset() + offset;
            return offset;
        }

        public long capacity() {
            return len;
        }

        public ModelByteBuffer getRoot() {
            return root;
        }

        public FileInfo getFile() {
            return file;
        }

        public long getFilePointer() {
            return fileoffset;
        }

        public static void loadAll(IList<ModelByteBuffer> col) {
            FileInfo selfile = null;
            FileStream raf = null;
            try {
                foreach (ModelByteBuffer mbuff in col) {
                    ModelByteBuffer mbuff0 = mbuff.root; //a@
                    if (mbuff0.file == null)
                        continue;
                    if (mbuff0.buffer != null)
                        continue;
                    if (selfile == null || !selfile.Equals(mbuff0.file)) {
                        if (raf != null) {
                            raf.Close();
                            raf = null;
                        }
                        selfile = mbuff0.file;
                        raf = new FileStream(mbuff0.file.FullName, FileMode.Open, FileAccess.Read);
                    }
                    raf.Seek(mbuff0.fileoffset, SeekOrigin.Begin);
                    byte[] buffer = new byte[(int)mbuff0.capacity()];

                    int read = 0;
                    int avail = buffer.Length;
                    while (read != avail) {
                        if (avail - read > 65536) {
                            raf.Read(buffer, read, 65536);
                            read += 65536;
                        } else {
                            raf.Read(buffer, read, avail - read);
                            read = avail;
                        }

                    }

                    mbuff0.buffer = buffer;
                    mbuff0.offset = 0;
                }
            } finally {
                if (raf != null)
                    raf.Close();
            }
        }

        public void load() {
            if (root != this) {
                root.load();
                return;
            }
            if (buffer != null)
                return;
            if (file == null) {
                throw new InvalidOperationException(
                        "No file associated with this ByteBuffer!");
            }

            using (BinaryReader istream = new BinaryReader(getInputStream())) {
                buffer = new byte[(int)capacity()];
                offset = 0;
                istream.Read(buffer, 0, buffer.Length);
            }
        }

        public void unload() {
            if (root != this) {
                root.unload();
                return;
            }
            if (file == null) {
                throw new InvalidOperationException(
                        "No file associated with this ByteBuffer!");
            }
            root.buffer = null;
        }
    }
}
