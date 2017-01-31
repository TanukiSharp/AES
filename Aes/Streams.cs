using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aes
{
    public static class InMemoryFile
    {
        internal static string PathToKey(string path)
        {
            return Path.GetFullPath(path).ToLowerInvariant();
        }

        public static bool Exists(string path)
        {
            var key = PathToKey(path);
            return InMemoryFileStream.fs.TryGetValue(key, out MemoryStream ignore) && ignore != null;
        }

        public static void Delete(string path)
        {
            InMemoryFileStream.fs[PathToKey(path)] = null;
        }

        public static byte[] ReadAllBytes(string path)
        {
            return InMemoryFileStream.fs[PathToKey(path)].ToArray();
        }

        public static string ReadAllText(string path)
        {
            return Encoding.ASCII.GetString(InMemoryFileStream.fs[PathToKey(path)].ToArray());
        }

        public static void WriteAllBytes(string path, byte[] content)
        {
            InMemoryFileStream.fs[PathToKey(path)] = new MemoryStream(content);
        }

        public static void WriteAllText(string path, string content)
        {
            InMemoryFileStream.fs[PathToKey(path)] = new MemoryStream(Encoding.ASCII.GetBytes(content));
        }
    }

    public class InMemoryFileStream : Stream
    {
        internal static readonly IDictionary<string, MemoryStream> fs = new Dictionary<string, MemoryStream>();

        private string path;
        private FileMode mode;
        private FileAccess access;

        public InMemoryFileStream(string path, FileMode mode, FileAccess access)
        {
            this.path = InMemoryFile.PathToKey(path);
            this.mode = mode;
            this.access = access;

            if (mode == FileMode.Create)
                fs[this.path] = new MemoryStream();
            else
            {
                if (fs.TryGetValue(this.path, out MemoryStream ms))
                    ms.Position = 0;
            }
        }

        public override bool CanRead => mode == FileMode.Open;

        public override bool CanSeek => mode == FileMode.Open;

        public override bool CanWrite => mode == FileMode.Create;

        public override long Length => fs[path].Length;

        public override long Position { get => fs[path].Position; set => fs[path].Position = value; }

        public override void Flush()
        {
            fs[path].Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return fs[path].Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return fs[path].Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            fs[path].SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            fs[path].Write(buffer, offset, count);
        }
    }

    public class TemporaryBase64OutStream : Stream
    {
        private MemoryStream bufferStream = new MemoryStream();
        private readonly Stream outputStream;

        public TemporaryBase64OutStream(Stream outputStream)
        {
            if (outputStream == null)
                throw new ArgumentNullException(nameof(outputStream));

            this.outputStream = outputStream;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            bufferStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            EncodeWriteAndClean();
        }

        private void EncodeWriteAndClean()
        {
            ArraySegment<byte> buffer;

            if (bufferStream.TryGetBuffer(out buffer) == false)
                buffer = new ArraySegment<byte>(bufferStream.ToArray());

            byte[] data = Encoding.ASCII.GetBytes(Convert.ToBase64String(buffer.Array, buffer.Offset, buffer.Count));

            if (data.Length == 0)
                return;

            outputStream.Write(data, 0, data.Length);

            bufferStream.Dispose();
            bufferStream = new MemoryStream();
        }
    }

    public class ConsoleOutStream : Stream
    {
        public static readonly string BeginDataMarker = "--- BEGIN DATA ---|";
        public static readonly string EndDataMarker = "|--- END DATA ---";

        private readonly Encoding encoding;
        private MemoryStream innerStream = new MemoryStream();

        public ConsoleOutStream()
            : this(Encoding.UTF8)
        {
        }

        public ConsoleOutStream(Encoding encoding)
        {
            if (encoding == null)
                throw new ArgumentNullException(nameof(encoding));

            this.encoding = encoding;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;

        public override long Length
        {
            get
            {
                throw new NotSupportedException();
            }
        }

        public override long Position
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            innerStream.Write(buffer, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            PrintAndClean();
        }

        private void PrintAndClean()
        {
            if (innerStream.Length > 0)
            {
                Console.WriteLine();
                Console.Write(BeginDataMarker);
                if (innerStream.TryGetBuffer(out ArraySegment<byte> buffer))
                    Console.Write(encoding.GetString(buffer.Array, buffer.Offset, buffer.Count));
                else
                    Console.Write(encoding.GetString(innerStream.ToArray()));
                Console.WriteLine(EndDataMarker);
            }

            innerStream.Dispose();
            innerStream = new MemoryStream();
        }

        internal static string TrimConsoleDataMarkers(string str)
        {
            int start = str.IndexOf(ConsoleOutStream.BeginDataMarker);
            if (start < 0)
                return str;

            start += ConsoleOutStream.BeginDataMarker.Length;

            int end = str.IndexOf(ConsoleOutStream.EndDataMarker, start);
            if (end < 0)
                return str;

            return str.Substring(start, end - start);
        }
    }

    public class HexadecimalStream : Stream
    {
        private readonly Stream innerStream;

        public HexadecimalStream(Stream innerStream)
        {
            if (innerStream == null)
                throw new ArgumentNullException(nameof(innerStream));

            this.innerStream = innerStream;
        }

        public override bool CanRead => innerStream.CanRead;
        public override bool CanSeek => innerStream.CanSeek;
        public override bool CanWrite => innerStream.CanWrite;

        public override long Length => innerStream.Length;

        public override long Position
        {
            get
            {
                return innerStream.Position;
            }
            set
            {
                innerStream.Position = value;
            }
        }

        public override void Flush()
        {
            innerStream.Flush();
        }

        // read hexadecimal characters and return bytes
        public override int Read(byte[] buffer, int offset, int count)
        {
            int i;

            for (i = 0; i < count; i++)
            {
                int c1 = innerStream.ReadByte();
                int c2 = innerStream.ReadByte();

                bool hasC1 = c1 > 0;
                bool hasC2 = c2 > 0;

                if (hasC1 == false && hasC2 == false)
                    break;
                else if (hasC1 != hasC2)
                    throw new FormatException("Invalid hexadecimal data length.");

                buffer[offset + i] = (byte)(Utils.HexCharToByte(c1) << 4 | Utils.HexCharToByte(c2));
            }

            return i;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            innerStream.SetLength(value);
        }

        // takes bytes and write hexadecimal characters
        public override void Write(byte[] buffer, int offset, int count)
        {
            int end = offset + count;

            for (int i = offset; i < end; i++)
            {
                innerStream.WriteByte(Utils.ByteToHexChar(buffer[i] >> 4));
                innerStream.WriteByte(Utils.ByteToHexChar(buffer[i] & 0xF));
            }
        }
    }
}
