/*
 * Copyright (c) 2008, 2021, Oracle and/or its affiliates. All rights reserved.
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

//import java.io.IOException;
//import java.io.InputStream;
//import java.util.ArrayList;
//import java.util.Arrays;
//import java.util.Objects;

//import javax.sound.sampled.AudioFormat;
//import javax.sound.sampled.AudioFormat.Encoding;
//import javax.sound.sampled.AudioInputStream;
//import javax.sound.sampled.AudioSystem;
//import javax.sound.sampled.spi.FormatConversionProvider;

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.CompilerServices;
using SystemX.Sound.Sampled;

namespace SystemX.Media.Sound {

/**
 * This class is used to convert between 8,16,24,32 bit signed/unsigned
 * big/little endian fixed/floating stereo/mono/multi-channel audio streams and
 * perform sample-rate conversion if needed.
 * 
 * @author Karl Helgason
 */
    public sealed class AudioFloatFormatConverter : FormatConversionProvider {

        private class AudioFloatFormatConverterInputStream : InputStream {
            private readonly AudioFloatConverter converter;

            private readonly AudioFloatInputStream stream;

            private float[] readfloatbuffer;

            private readonly int fsize;

            internal AudioFloatFormatConverterInputStream(AudioFormat targetFormat,
                                                          AudioFloatInputStream stream) {
                this.stream = stream;
                converter = AudioFloatConverter.getConverter(targetFormat);
                fsize = ((targetFormat.getSampleSizeInBits() + 7) / 8);
            }

            public override long Position { //a@ todo
                get { return stream.getFrameLength() - stream.available(); }
            }

            public override long Length { //a@ todo
                get { return stream.getFrameLength(); }
            }

            public override int ReadByte() {
                byte[] b = new byte[1];
                int ret = Read(b, 0, b.Length);
                if (ret <= 0)
                    return -1;
                return b[0] & 0xFF;
            }

            public override int Read(byte[] b, int off, int len) {

                int flen = len / fsize;
                if (readfloatbuffer == null || readfloatbuffer.Length < flen)
                    readfloatbuffer = new float[flen];
                int ret = stream.read(readfloatbuffer, 0, flen);
                if (ret <= 0)
                    return 0;
                converter.toByteArray(readfloatbuffer, 0, ret, b, off);
                return ret * fsize;
            }

            public override int available() {
                int ret = stream.available();
                if (ret <= 0)
                    return ret;
                return ret * fsize;
            }

            public override void Close() {
                stream.close();
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void mark(int readlimit) {
                stream.mark(readlimit * fsize);
            }

            public override bool markSupported() {
                return stream.markSupported();
            }

            [MethodImpl(MethodImplOptions.Synchronized)]
            public override void reset() {
                stream.reset();
            }

            public override long skip(long n) {
                long ret = stream.skip(n / fsize);
                if (ret < 0)
                    return ret;
                return ret * fsize;
            }

        }

        private class AudioFloatInputStreamChannelMixer :
                AudioFloatInputStream {

            private readonly int targetChannels;

            private readonly int sourceChannels;

            private readonly AudioFloatInputStream ais;

            private readonly AudioFormat targetFormat;

            private float[] conversion_buffer;

            internal AudioFloatInputStreamChannelMixer(AudioFloatInputStream ais,
                    int targetChannels) {
                this.sourceChannels = ais.getFormat().getChannels();
                this.targetChannels = targetChannels;
                this.ais = ais;
                AudioFormat format = ais.getFormat();
                targetFormat = new AudioFormat(format.getEncoding(), format
                        .getSampleRate(), format.getSampleSizeInBits(),
                        targetChannels, (format.getFrameSize() / sourceChannels)
                                * targetChannels, format.getFrameRate(), format
                                .isBigEndian());
            }

            public override int available() {
                return (ais.available() / sourceChannels) * targetChannels;
            }

            public override void close() {
                ais.close();
            }

            public override AudioFormat getFormat() {
                return targetFormat;
            }

            public override long getFrameLength() {
                return ais.getFrameLength();
            }

            public override void mark(int readlimit) {
                ais.mark((readlimit / targetChannels) * sourceChannels);
            }

            public override bool markSupported() {
                return ais.markSupported();
            }

            public override int read(float[] b, int off, int len) {
                int len2 = (len / targetChannels) * sourceChannels;
                if (conversion_buffer == null || conversion_buffer.Length < len2)
                    conversion_buffer = new float[len2];
                int ret = ais.read(conversion_buffer, 0, len2);
                if (ret < 0)
                    return ret;
                if (sourceChannels == 1) {
                    int cs = targetChannels;
                    for (int c = 0; c < targetChannels; c++) {
                        for (int i = 0, ix = off + c; i < len2; i++, ix += cs) {
                            b[ix] = conversion_buffer[i];
                        }
                    }
                } else if (targetChannels == 1) {
                    int cs = sourceChannels;
                    for (int i = 0, ix = off; i < len2; i += cs, ix++) {
                        b[ix] = conversion_buffer[i];
                    }
                    for (int c = 1; c < sourceChannels; c++) {
                        for (int i = c, ix = off; i < len2; i += cs, ix++) {
                            b[ix] += conversion_buffer[i];
                        }
                    }
                    float vol = 1f / ((float)sourceChannels);
                    for (int i = 0, ix = off; i < len2; i += cs, ix++) {
                        b[ix] *= vol;
                    }
                } else {
                    int minChannels = Math.Min(sourceChannels, targetChannels);
                    int off_len = off + len;
                    int ct = targetChannels;
                    int cs = sourceChannels;
                    for (int c = 0; c < minChannels; c++) {
                        for (int i = off + c, ix = c; i < off_len; i += ct, ix += cs) {
                            b[i] = conversion_buffer[ix];
                        }
                    }
                    for (int c = minChannels; c < targetChannels; c++) {
                        for (int i = off + c; i < off_len; i += ct) {
                            b[i] = 0;
                        }
                    }
                }
                return (ret / sourceChannels) * targetChannels;
            }

            public override void reset() {
                ais.reset();
            }

            public override long skip(long len) {
                long ret = ais.skip((len / targetChannels) * sourceChannels);
                if (ret < 0)
                    return ret;
                return (ret / sourceChannels) * targetChannels;
            }

        }

        private class AudioFloatInputStreamResampler :
                AudioFloatInputStream {

            private readonly AudioFloatInputStream ais;

            private readonly AudioFormat targetFormat;

            private float[] skipbuffer;

            private SoftAbstractResampler resampler;

            private readonly float[] pitch = new float[1];

            private readonly float[] ibuffer2;

            private readonly float[][] ibuffer;

            private float ibuffer_index = 0;

            private int ibuffer_len = 0;

            private readonly int nrofchannels;

            private float[][] cbuffer;

            private readonly int buffer_len = 512;

            private readonly int pad;

            private readonly int pad2;

            private readonly float[] ix = new float[1];

            private readonly int[] ox = new int[1];

            private float[][] mark_ibuffer = null;

            private float mark_ibuffer_index = 0;

            private int mark_ibuffer_len = 0;

            internal AudioFloatInputStreamResampler(AudioFloatInputStream ais,
                                                    AudioFormat format) {
                this.ais = ais;
                AudioFormat sourceFormat = ais.getFormat();
                targetFormat = new AudioFormat(sourceFormat.getEncoding(), format
                        .getSampleRate(), sourceFormat.getSampleSizeInBits(),
                        sourceFormat.getChannels(), sourceFormat.getFrameSize(),
                        format.getSampleRate(), sourceFormat.isBigEndian());
                nrofchannels = targetFormat.getChannels();
                Object interpolation = format.getProperty("interpolation");
                if (interpolation is String resamplerType) {
                    if (resamplerType.Equals("point", StringComparison.OrdinalIgnoreCase))
                        this.resampler = new SoftPointResampler();
                    if (resamplerType.Equals("linear", StringComparison.OrdinalIgnoreCase))
                        this.resampler = new SoftLinearResampler2();
                    if (resamplerType.Equals("linear1", StringComparison.OrdinalIgnoreCase))
                        this.resampler = new SoftLinearResampler();
                    if (resamplerType.Equals("linear2", StringComparison.OrdinalIgnoreCase))
                        this.resampler = new SoftLinearResampler2();
                    if (resamplerType.Equals("cubic", StringComparison.OrdinalIgnoreCase))
                        this.resampler = new SoftCubicResampler();
                    if (resamplerType.Equals("lanczos", StringComparison.OrdinalIgnoreCase))
                        this.resampler = new SoftLanczosResampler();
                    if (resamplerType.Equals("sinc", StringComparison.OrdinalIgnoreCase))
                        this.resampler = new SoftSincResampler();
                }
                if (resampler == null)
                    resampler = new SoftLinearResampler2(); // new
                // SoftLinearResampler2();
                pitch[0] = sourceFormat.getSampleRate() / format.getSampleRate();
                pad = resampler.getPadding();
                pad2 = pad * 2;
                ibuffer = new float[nrofchannels][];
                for (int i = 0; i < ibuffer.Length; i++) {
                    ibuffer[i] = new float[buffer_len + pad2];
                }
                ibuffer2 = new float[nrofchannels * buffer_len];
                ibuffer_index = buffer_len + pad;
                ibuffer_len = buffer_len;
            }

            public override int available() {
                return 0;
            }

            public override void close() {
                ais.close();
            }

            public override AudioFormat getFormat() {
                return targetFormat;
            }

            public override long getFrameLength() {
                return AudioSystem.NOT_SPECIFIED; // ais.getFrameLength();
            }

            public override void mark(int readlimit) {
                ais.mark((int)(readlimit * pitch[0]));
                mark_ibuffer_index = ibuffer_index;
                mark_ibuffer_len = ibuffer_len;
                if (mark_ibuffer == null) {
                    mark_ibuffer = new float[ibuffer.Length][];
                    for (int i = 0; i < mark_ibuffer.Length; i++) {
                        mark_ibuffer[i] = new float[ibuffer[0].Length];
                    }
                }
                for (int c = 0; c < ibuffer.Length; c++) {
                    float[] from = ibuffer[c];
                    float[] to = mark_ibuffer[c];
                    for (int i = 0; i < to.Length; i++) {
                        to[i] = from[i];
                    }
                }
            }

            public override bool markSupported() {
                return ais.markSupported();
            }

            private void readNextBuffer() {

                if (ibuffer_len == -1)
                    return;

                for (int c = 0; c < nrofchannels; c++) {
                    float[] buff = ibuffer[c];
                    int buffer_len_pad = ibuffer_len + pad2;
                    for (int i = ibuffer_len, ix = 0; i < buffer_len_pad; i++, ix++) {
                        buff[ix] = buff[i];
                    }
                }

                ibuffer_index -= (ibuffer_len);

                ibuffer_len = ais.read(ibuffer2);
                if (ibuffer_len >= 0) {
                    while (ibuffer_len < ibuffer2.Length) {
                        int ret = ais.read(ibuffer2, ibuffer_len, ibuffer2.Length
                                - ibuffer_len);
                        if (ret == -1)
                            break;
                        ibuffer_len += ret;
                    }
                    Array.Clear(ibuffer2, ibuffer_len, ibuffer2.Length - ibuffer_len);
                    ibuffer_len /= nrofchannels;
                } else {
                    Array.Clear(ibuffer2, 0, ibuffer2.Length);
                }

                int ibuffer2_len = ibuffer2.Length;
                for (int c = 0; c < nrofchannels; c++) {
                    float[] buff = ibuffer[c];
                    for (int i = c, ix = pad2; i < ibuffer2_len; i += nrofchannels, ix++) {
                        buff[ix] = ibuffer2[i];
                    }
                }
            }

            public override int read(float[] b, int off, int len) {

                if (cbuffer == null || cbuffer[0].Length < len / nrofchannels) {
                    cbuffer = new float[nrofchannels][];
                    for (int i = 0; i < cbuffer.Length; i++) {
                        cbuffer[i] = new float[len / nrofchannels];
                    }
                }
                if (ibuffer_len == -1)
                    return -1;
                if (len < 0)
                    return 0;
                int offlen = off + len;
                int remain = len / nrofchannels;
                int destPos = 0;
                int in_end = ibuffer_len;
                while (remain > 0) {
                    if (ibuffer_len >= 0) {
                        if (ibuffer_index >= (ibuffer_len + pad))
                            readNextBuffer();
                        in_end = ibuffer_len + pad;
                    }

                    if (ibuffer_len < 0) {
                        in_end = pad2;
                        if (ibuffer_index >= in_end)
                            break;
                    }

                    if (ibuffer_index < 0)
                        break;
                    int preDestPos = destPos;
                    for (int c = 0; c < nrofchannels; c++) {
                        ix[0] = ibuffer_index;
                        ox[0] = destPos;
                        float[] buff = ibuffer[c];
                        resampler.interpolate(buff, ix, in_end, pitch, 0,
                                cbuffer[c], ox, len / nrofchannels);
                    }
                    ibuffer_index = ix[0];
                    destPos = ox[0];
                    remain -= destPos - preDestPos;
                }
                for (int c = 0; c < nrofchannels; c++) {
                    int ix = 0;
                    float[] buff = cbuffer[c];
                    for (int i = c + off; i < offlen; i += nrofchannels) {
                        b[i] = buff[ix++];
                    }
                }
                return len - remain * nrofchannels;
            }

            public override void reset() {
                ais.reset();
                if (mark_ibuffer == null)
                    return;
                ibuffer_index = mark_ibuffer_index;
                ibuffer_len = mark_ibuffer_len;
                for (int c = 0; c < ibuffer.Length; c++) {
                    float[] from = mark_ibuffer[c];
                    float[] to = ibuffer[c];
                    for (int i = 0; i < to.Length; i++) {
                        to[i] = from[i];
                    }
                }
            }

            public override long skip(long len) {
                if (len < 0)
                    return 0;
                if (skipbuffer == null)
                    skipbuffer = new float[1024 * targetFormat.getFrameSize()];
                float[] l_skipbuffer = skipbuffer;
                long remain = len;
                while (remain > 0) {
                    int ret = read(l_skipbuffer, 0, (int)Math.Min(remain,
                            skipbuffer.Length));
                    if (ret < 0) {
                        if (remain == len)
                            return ret;
                        break;
                    }
                    remain -= ret;
                }
                return len - remain;
            }
        }

        private readonly AudioFormat.Encoding[] formats = { AudioFormat.Encoding.PCM_SIGNED,
                                                              AudioFormat.Encoding.PCM_UNSIGNED,
                                                              AudioFormat.Encoding.PCM_FLOAT};

        public override AudioInputStream getAudioInputStream(AudioFormat.Encoding targetEncoding,
                                                             AudioInputStream sourceStream) {
            if (!isConversionSupported(targetEncoding, sourceStream.getFormat())) {
                throw new ArgumentException(
                        "Unsupported conversion: " + sourceStream.getFormat()
                                .ToString() + " to " + targetEncoding.ToString());
            }
            if (sourceStream.getFormat().getEncoding().Equals(targetEncoding))
                return sourceStream;
            AudioFormat format = sourceStream.getFormat();
            int channels = format.getChannels();
            AudioFormat.Encoding encoding = targetEncoding;
            float samplerate = format.getSampleRate();
            int bits = format.getSampleSizeInBits();
            bool bigendian = format.isBigEndian();
            if (targetEncoding.Equals(AudioFormat.Encoding.PCM_FLOAT))
                bits = 32;
            AudioFormat targetFormat = new AudioFormat(encoding, samplerate, bits,
                    channels, channels * bits / 8, samplerate, bigendian);
            return getAudioInputStream(targetFormat, sourceStream);
        }

        public override AudioInputStream getAudioInputStream(AudioFormat targetFormat,
                                                             AudioInputStream sourceStream) {
            if (!isConversionSupported(targetFormat, sourceStream.getFormat()))
                throw new ArgumentException("Unsupported conversion: "
                        + sourceStream.getFormat().ToString() + " to "
                        + targetFormat.ToString());
            return getAudioInputStream(targetFormat, AudioFloatInputStream
                    .getInputStream(sourceStream));
        }

        public AudioInputStream getAudioInputStream(AudioFormat targetFormat,
                                                    AudioFloatInputStream sourceStream) {

            if (!isConversionSupported(targetFormat, sourceStream.getFormat()))
                throw new ArgumentException("Unsupported conversion: "
                        + sourceStream.getFormat().ToString() + " to "
                        + targetFormat.ToString());
            if (targetFormat.getChannels() != sourceStream.getFormat()
                    .getChannels())
                sourceStream = new AudioFloatInputStreamChannelMixer(sourceStream,
                        targetFormat.getChannels());
            if (Math.Abs(targetFormat.getSampleRate()
                    - sourceStream.getFormat().getSampleRate()) > 0.000001)
                sourceStream = new AudioFloatInputStreamResampler(sourceStream,
                        targetFormat);
            return new AudioInputStream(new AudioFloatFormatConverterInputStream(
                    targetFormat, sourceStream), targetFormat, sourceStream
                    .getFrameLength());
        }

        public override AudioFormat.Encoding[] getSourceEncodings() {
            return new AudioFormat.Encoding[] { AudioFormat.Encoding.PCM_SIGNED, AudioFormat.Encoding.PCM_UNSIGNED,
                AudioFormat.Encoding.PCM_FLOAT };
        }

        public override AudioFormat.Encoding[] getTargetEncodings() {
            return getSourceEncodings();
        }

        public override AudioFormat.Encoding[] getTargetEncodings(AudioFormat sourceFormat) {
            if (AudioFloatConverter.getConverter(sourceFormat) == null)
                return new AudioFormat.Encoding[0];
            return new AudioFormat.Encoding[] { AudioFormat.Encoding.PCM_SIGNED, AudioFormat.Encoding.PCM_UNSIGNED,
                AudioFormat.Encoding.PCM_FLOAT };
        }

        public override AudioFormat[] getTargetFormats(AudioFormat.Encoding targetEncoding,
                                                       AudioFormat sourceFormat) {
            if (targetEncoding == null) {
                throw new ArgumentNullException(nameof(targetEncoding));
            }
            if (AudioFloatConverter.getConverter(sourceFormat) == null)
                return new AudioFormat[0];
            int channels = sourceFormat.getChannels();

            List<AudioFormat> formats = new List<AudioFormat>();

            if (targetEncoding.Equals(AudioFormat.Encoding.PCM_SIGNED))
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                        AudioSystem.NOT_SPECIFIED, 8, channels, channels,
                        AudioSystem.NOT_SPECIFIED, false));
            if (targetEncoding.Equals(AudioFormat.Encoding.PCM_UNSIGNED))
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                        AudioSystem.NOT_SPECIFIED, 8, channels, channels,
                        AudioSystem.NOT_SPECIFIED, false));

            for (int bits = 16; bits < 32; bits += 8) {
                if (targetEncoding.Equals(AudioFormat.Encoding.PCM_SIGNED)) {
                    formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                            AudioSystem.NOT_SPECIFIED, bits, channels, channels
                                    * bits / 8, AudioSystem.NOT_SPECIFIED, false));
                    formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_SIGNED,
                            AudioSystem.NOT_SPECIFIED, bits, channels, channels
                                    * bits / 8, AudioSystem.NOT_SPECIFIED, true));
                }
                if (targetEncoding.Equals(AudioFormat.Encoding.PCM_UNSIGNED)) {
                    formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                            AudioSystem.NOT_SPECIFIED, bits, channels, channels
                                    * bits / 8, AudioSystem.NOT_SPECIFIED, true));
                    formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_UNSIGNED,
                            AudioSystem.NOT_SPECIFIED, bits, channels, channels
                                    * bits / 8, AudioSystem.NOT_SPECIFIED, false));
                }
            }

            if (targetEncoding.Equals(AudioFormat.Encoding.PCM_FLOAT)) {
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_FLOAT,
                        AudioSystem.NOT_SPECIFIED, 32, channels, channels * 4,
                        AudioSystem.NOT_SPECIFIED, false));
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_FLOAT,
                        AudioSystem.NOT_SPECIFIED, 32, channels, channels * 4,
                        AudioSystem.NOT_SPECIFIED, true));
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_FLOAT,
                        AudioSystem.NOT_SPECIFIED, 64, channels, channels * 8,
                        AudioSystem.NOT_SPECIFIED, false));
                formats.Add(new AudioFormat(AudioFormat.Encoding.PCM_FLOAT,
                        AudioSystem.NOT_SPECIFIED, 64, channels, channels * 8,
                        AudioSystem.NOT_SPECIFIED, true));
            }

            return formats.ToArray();
        }

        public override bool isConversionSupported(AudioFormat targetFormat,
                                                   AudioFormat sourceFormat) {
            if (targetFormat == null) {
                throw new ArgumentNullException(nameof(targetFormat));
            }
            if (AudioFloatConverter.getConverter(sourceFormat) == null)
                return false;
            if (AudioFloatConverter.getConverter(targetFormat) == null)
                return false;
            if (sourceFormat.getChannels() <= 0)
                return false;
            if (targetFormat.getChannels() <= 0)
                return false;
            return true;
        }

        public override bool isConversionSupported(AudioFormat.Encoding targetEncoding,
                                                   AudioFormat sourceFormat) {
            if (targetEncoding == null) {
                throw new ArgumentNullException(nameof(targetEncoding));
            }
            if (AudioFloatConverter.getConverter(sourceFormat) == null)
                return false;
            for (int i = 0; i < formats.Length; i++) {
                if (targetEncoding.Equals(formats[i]))
                    return true;
            }
            return false;
        }
    }
}
