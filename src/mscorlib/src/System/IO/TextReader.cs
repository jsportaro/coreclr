// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Abstract base class for all Text-only Readers.
** Subclasses will include StreamReader & StringReader.
**
**
===========================================================*/

using System.Text;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO {
    // This abstract base class represents a reader that can read a sequential
    // stream of characters.  This is not intended for reading bytes -
    // there are methods on the Stream class to read bytes.
    // A subclass must minimally implement the Peek() and Read() methods.
    //
    // This class is intended for character input, not bytes.  
    // There are methods on the Stream class for reading bytes. 
    internal abstract class TextReader : MarshalByRefObject, IDisposable {
        public static readonly TextReader Null = new NullTextReader();
    
        protected TextReader() {}
    
        // Closes this TextReader and releases any system resources associated with the
        // TextReader. Following a call to Close, any operations on the TextReader
        // may raise exceptions.
        // 
        // This default method is empty, but descendant classes can override the
        // method to provide the appropriate functionality.
        public virtual void Close() 
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        // Returns the next available character without actually reading it from
        // the input stream. The current position of the TextReader is not changed by
        // this operation. The returned value is -1 if no further characters are
        // available.
        // 
        // This default method simply returns -1.
        //
        public virtual int Peek() 
        {
            return -1;
        }
    
        // Reads the next character from the input stream. The returned value is
        // -1 if no further characters are available.
        // 
        // This default method simply returns -1.
        //
        public virtual int Read()
        {
            return -1;
        }
    
        // Reads a block of characters. This method will read up to
        // count characters from this TextReader into the
        // buffer character array starting at position
        // index. Returns the actual number of characters read.
        //
        public virtual int Read(char[] buffer, int index, int count) 
        {
            if (buffer==null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);
    
            int n = 0;
            do {
                int ch = Read();
                if (ch == -1) break;
                buffer[index + n++] = (char)ch;
            } while (n < count);
            return n;
        }

        // Reads all characters from the current position to the end of the 
        // TextReader, and returns them as one string.
        public virtual String ReadToEnd()
        {
            char[] chars = new char[4096];
            int len;
            StringBuilder sb = new StringBuilder(4096);
            while((len=Read(chars, 0, chars.Length)) != 0) 
            {
                sb.Append(chars, 0, len);
            }
            return sb.ToString();
        }

        // Blocking version of read.  Returns only when count
        // characters have been read or the end of the file was reached.
        // 
        public virtual int ReadBlock(char[] buffer, int index, int count) 
        {
            int i, n = 0;
            do {
                n += (i = Read(buffer, index + n, count - n));
            } while (i > 0 && n < count);
            return n;
        }

        // Reads a line. A line is defined as a sequence of characters followed by
        // a carriage return ('\r'), a line feed ('\n'), or a carriage return
        // immediately followed by a line feed. The resulting string does not
        // contain the terminating carriage return and/or line feed. The returned
        // value is null if the end of the input stream has been reached.
        //
        public virtual String ReadLine() 
        {
            StringBuilder sb = new StringBuilder();
            while (true) {
                int ch = Read();
                if (ch == -1) break;
                if (ch == '\r' || ch == '\n') 
                {
                    if (ch == '\r' && Peek() == '\n') Read();
                    return sb.ToString();
                }
                sb.Append((char)ch);
            }
            if (sb.Length > 0) return sb.ToString();
            return null;
        }

        #region Task based Async APIs
        public virtual Task<String> ReadLineAsync()
        {
            return Task<String>.Factory.StartNew(state =>
            {
                return ((TextReader)state).ReadLine();
            },
            this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public async virtual Task<String> ReadToEndAsync()
        {
            char[] chars = new char[4096];
            int len;
            StringBuilder sb = new StringBuilder(4096);
            while((len = await ReadAsyncInternal(chars, 0, chars.Length).ConfigureAwait(false)) != 0) 
            {
                sb.Append(chars, 0, len);
            }
            return sb.ToString();
        }

        public virtual Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            if (buffer==null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);

            return ReadAsyncInternal(buffer, index, count);
        }

        internal virtual Task<int> ReadAsyncInternal(char[] buffer, int index, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(index >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(buffer.Length - index >= count);

            var tuple = new Tuple<TextReader, char[], int, int>(this, buffer, index, count);
            return Task<int>.Factory.StartNew(state =>
            {
                var t = (Tuple<TextReader, char[], int, int>)state;
                return t.Item1.Read(t.Item2, t.Item3, t.Item4);
            },
            tuple, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public virtual Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            if (buffer==null)
                throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
            if (index < 0 || count < 0)
                throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);
            if (buffer.Length - index < count)
                throw new ArgumentException(SR.Argument_InvalidOffLen);


            return ReadBlockAsyncInternal(buffer, index, count);
         }

        private async Task<int> ReadBlockAsyncInternal(char[] buffer, int index, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(index >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(buffer.Length - index >= count);

            int i, n = 0;
            do
            {
                i = await ReadAsyncInternal(buffer, index + n, count - n).ConfigureAwait(false);
                n += i;
            } while (i > 0 && n < count);

            return n;
        }
        #endregion

        public static TextReader Synchronized(TextReader reader) 
        {
            if (reader==null)
                throw new ArgumentNullException(nameof(reader));

            if (reader is SyncTextReader)
                return reader;
            
            return new SyncTextReader(reader);
        }
        
        private sealed class NullTextReader : TextReader
        {
            public NullTextReader(){}

            public override int Read(char[] buffer, int index, int count) 
            {
                return 0;
            }
            
            public override String ReadLine() 
            {
                return null;
            }
        }
        
        internal sealed class SyncTextReader : TextReader 
        {
            internal TextReader _in;
            
            internal SyncTextReader(TextReader t) 
            {
                _in = t;        
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override void Close() 
            {
                // So that any overriden Close() gets run
                _in.Close();
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            protected override void Dispose(bool disposing) 
            {
                // Explicitly pick up a potentially methodimpl'ed Dispose
                if (disposing)
                    ((IDisposable)_in).Dispose();
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override int Peek() 
            {
                return _in.Peek();
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override int Read() 
            {
                return _in.Read();
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override int Read(char[] buffer, int index, int count) 
            {
                return _in.Read(buffer, index, count);
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override int ReadBlock(char[] buffer, int index, int count) 
            {
                return _in.ReadBlock(buffer, index, count);
            }
            
            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override String ReadLine() 
            {
                return _in.ReadLine();
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override String ReadToEnd() 
            {
                return _in.ReadToEnd();
            }

            //
            // On SyncTextReader all APIs should run synchronously, even the async ones.
            //

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override Task<String> ReadLineAsync()
            {
                return Task.FromResult(ReadLine());
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override Task<String> ReadToEndAsync()
            {
                return Task.FromResult(ReadToEnd());
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
            {
                if (buffer==null)
                    throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);
                if (buffer.Length - index < count)
                    throw new ArgumentException(SR.Argument_InvalidOffLen);


                return Task.FromResult(ReadBlock(buffer, index, count));
            }

            [MethodImplAttribute(MethodImplOptions.Synchronized)]
            public override Task<int> ReadAsync(char[] buffer, int index, int count)
            {
                if (buffer==null)
                    throw new ArgumentNullException(nameof(buffer), SR.ArgumentNull_Buffer);
                if (index < 0 || count < 0)
                    throw new ArgumentOutOfRangeException((index < 0 ? nameof(index) : nameof(count)), SR.ArgumentOutOfRange_NeedNonNegNum);
                if (buffer.Length - index < count)
                    throw new ArgumentException(SR.Argument_InvalidOffLen);

                return Task.FromResult(Read(buffer, index, count));
            }
        }
    }
}
