using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

namespace System.IO {
    public static class StreamEx {
        public static long addExact(long x, long y) {
            long r;
            try {
                checked {
                    r = x + y;
                }
            } catch (OverflowException) {
                throw new ArithmeticException("long overflow");
            }
            // HD 2-12 Overflow iff both arguments have the opposite sign of the result
            //if (((x ^ r) & (y ^ r)) < 0) {
            //    throw new ArithmeticException("long overflow");
            //}
            return r;
        }

        private const int DEFAULT_BUFFER_SIZE = 16384;

        /**
         * Reads all bytes from this input stream and writes the bytes to the
         * given output stream in the order that they are read. On return, this
         * input stream will be at end of stream. This method does not close either
         * stream.
         * <p>
         * This method may block indefinitely reading from the input stream, or
         * writing to the output stream. The behavior for the case where the input
         * and/or output stream is <i>asynchronously closed</i>, or the thread
         * interrupted during the transfer, is highly input and output stream
         * specific, and therefore not specified.
         * <p>
         * If the total number of bytes transferred is greater than {@linkplain
         * Long#MAX_VALUE}, then {@code Long.MAX_VALUE} will be returned.
         * <p>
         * If an I/O error occurs reading from the input stream or writing to the
         * output stream, then it may do so after some bytes have been read or
         * written. Consequently the input stream may not be at end of stream and
         * one, or both, streams may be in an inconsistent state. It is strongly
         * recommended that both streams be promptly closed if an I/O error occurs.
         *
         * @param  out the output stream, non-null
         * @return the number of bytes transferred
         * @throws IOException if an I/O error occurs when reading or writing
         * @throws NullPointerException if {@code out} is {@code null}
         *
         * @since 9
         */
        public static long transferTo(this Stream input, Stream output) {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            long transferred = 0;
            byte[] buffer = new byte[DEFAULT_BUFFER_SIZE];
            int read;
            while ((read = input.Read(buffer, 0, DEFAULT_BUFFER_SIZE)) >= 0) {
                output.Write(buffer, 0, read);
                if (transferred < long.MaxValue) {
                    try {
                        transferred = addExact(transferred, read);
                    } catch (ArithmeticException) {
                        transferred = long.MaxValue;
                    }
                }
            }
            return transferred;
        }

    }

    public abstract class InputStream : Stream {

        // SKIP_BUFFER_SIZE is used to determine the size of skipBuffer
        private const int SKIP_BUFFER_SIZE = 2048;
        // skipBuffer is initialized in skip(long), if needed.
        private static byte[] skipBuffer;

        public virtual int available() {
            return 0;
        }

        public virtual long skip(long n) {
            long remaining = n;
            int nr;
            if (skipBuffer == null)
                skipBuffer = new byte[SKIP_BUFFER_SIZE];

            byte[] localSkipBuffer = skipBuffer;

            if (n <= 0) {
                return 0;
            }

            while (remaining > 0) {
                nr = Read(localSkipBuffer, 0,
                      (int)Math.Min(SKIP_BUFFER_SIZE, remaining));
                if (nr <= 0) {
                    break;
                }
                remaining -= nr;
            }

            return n - remaining;
        }

        public virtual bool markSupported() {
            return false;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void mark(int readlimit) {
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public virtual void reset() {
            throw new IOException("mark/reset not supported");
        }

        public virtual int Read(byte[] buffer) {
            return Read(buffer, 0, buffer.Length);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            throw new NotSupportedException("InputStream Write not supported");
        }

        public override void WriteByte(byte value) {
            throw new NotSupportedException("InputStream WriteByte not supported");
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            throw new NotSupportedException("InputStream BeginWrite not supported");
        }

        public override void EndWrite(IAsyncResult asyncResult) {
            throw new NotSupportedException("InputStream EndWrite not supported");
        }

        public override void Flush() {
            //throw new NotSupportedException();
        }

        public override void SetLength(long value) {
            throw new NotSupportedException("InputStream SetLength not supported");
        }

        public override long Seek(long offset, SeekOrigin origin) {
            throw new NotSupportedException("InputStream Seek not supported");
        }

        public override long Position {
            set { throw new NotSupportedException("InputStream set_Position not supported"); }
        }

        public override bool CanRead {
            get { return true; }
        }

        public override bool CanWrite {
            get { return false; }
        }

        public override bool CanSeek {
            get { return false; }
        }

    }

    public class InputStreamImpl : InputStream {
        Stream stream;

        public InputStreamImpl(Stream stream) {
            if (stream == null) {
                throw new ArgumentNullException(nameof(stream));
            }
            this.stream = stream;
        }

        public Stream BaseStream {
            get { return stream; }
            private set { stream = value; }
        }

        public override void Write(byte[] buffer, int offset, int count) {
            stream.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value) {
            stream.WriteByte(value);
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            return stream.BeginWrite(buffer, offset, count, callback, state);
        }

        public override void EndWrite(IAsyncResult asyncResult) {
            stream.EndWrite(asyncResult);
        }

        public override void Flush() {
            stream.Flush();
        }

        public override bool CanRead {
            get { return stream.CanRead; }
        }

        public override bool CanWrite {
            get { return stream.CanWrite; }
        }

        public override bool CanTimeout {
            get { return stream.CanTimeout; }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state) {
            return stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override bool CanSeek {
            get { return stream.CanSeek; }
        }

        public override void Close() {
            stream.Close();
        }

        //protected override void Dispose(bool disposing) {
        //    base.Dispose(disposing);
        //}

        //[Obsolete]
        //protected override WaitHandle CreateWaitHandle() {
        //    return base.CreateWaitHandle();
        //}

        public override int EndRead(IAsyncResult asyncResult) {
            return stream.EndRead(asyncResult);
        }

        public override bool Equals(object obj) {
            return stream.Equals(obj);
        }

        public override int GetHashCode() {
            return stream.GetHashCode();
        }

        public override long Length {
            get { return stream.Length; }
        }

        public override long Position {
            get {
                return stream.Position;
            }
            set {
                stream.Position = value;
            }
        }

        public override int Read(byte[] buffer) {
            return stream.Read(buffer, 0, buffer.Length);
        }

        public override int Read(byte[] buffer, int offset, int count) {
            return stream.Read(buffer, offset, count);
        }

        public override int ReadByte() {
            return stream.ReadByte();
        }

        public override int ReadTimeout {
            get {
                return stream.ReadTimeout;
            }
            set {
                stream.ReadTimeout = value;
            }
        }

        public override long Seek(long offset, SeekOrigin origin) {
            return stream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            stream.SetLength(value);
        }

        public override String ToString() {
            return stream.ToString();
        }

        public override int WriteTimeout {
            get {
                return stream.WriteTimeout;
            }
            set {
                stream.WriteTimeout = value;
            }
        }

        public override int available() {
            InputStream iStream = stream as InputStream;
            if (iStream != null) {
                return iStream.available();
            } else {
                lock (stream) {
                    return (int)(stream.Length - stream.Position);
                }
            }
        }

        private int readlimit;
        private long markPosition;

        public override void mark(int readlimit) {
            InputStream iStream = stream as InputStream;
            if (iStream != null) {
                iStream.mark(readlimit);
            } else {
                this.readlimit = readlimit;
                this.markPosition = stream.Position;
            }
        }

        public override bool markSupported() {
            InputStream iStream = stream as InputStream;
            if (iStream != null) {
                return iStream.markSupported();
            } else {
                return true;
            }
        }

        public override void reset() {
            InputStream iStream = stream as InputStream;
            if (iStream != null) {
                iStream.reset();
            } else {
                //if (markSupported()) {
                //    if (stream.Position <= markPosition + readlimit) {
                lock (stream) {
                    stream.Position = markPosition;
                    return;
                }
                //} else {
                //}
                //}
            }
        }

        public override long skip(long n) {
            InputStream iStream = stream as InputStream;
            if (iStream != null) {
                return iStream.skip(n);
            } else {
                lock (stream) {
                    if (stream.Position + n > stream.Length) {
                        n = stream.Length - stream.Position;
                    }
                    if (n < 0) {
                        return 0;
                    }
                    stream.Position += n;
                    return n;
                }
            }
        }
    }
}
