#if DEBUG
#define IN_MEMORY
#endif

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Aes
{
    public static class Utils
    {
        public static string EnsureFilename(string filename)
        {
            if (Path.IsPathRooted(filename) == false)
                filename = Path.Combine(Directory.GetCurrentDirectory(), filename);

            return Path.GetFullPath(filename);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte HexCharToByte(int c)
        {
            if (c >= 'a' && c <= 'f')
                return (byte)((c - 'a') + 10);
            else if (c >= 'A' && c <= 'F')
                return (byte)((c - 'A') + 10);
            else if (c >= '0' && c <= '9')
                return (byte)(c - '0');
            else
                throw new ArgumentException($"Invalid '{nameof(c)}' argument.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ByteToHexChar(int b)
        {
            if (b <= 9)
                return (byte)(b + '0');
            else if (b <= 15)
                return (byte)(b - 10 + 'a');
            else
                throw new ArgumentException($"Invalid '{nameof(b)}' argument.");
        }

        public static bool TryGetFormat(string format, out DataFormat dataFormat)
        {
            dataFormat = DataFormat.None;

            if (StringEquals(format, "hex") || StringEquals(format, "hexa"))
                dataFormat = DataFormat.Hexa;
            else if (StringEquals(format, "b64") || StringEquals(format, "base64"))
                dataFormat = DataFormat.Base64;
            else if (StringEquals(format, "raw"))
                dataFormat = DataFormat.Raw;
            else
                return false;

            return true;
        }

        public static bool CheckFile(string filename)
        {
#if IN_MEMORY
            if (InMemoryFile.Exists(filename) == false)
#else
            if (File.Exists(filename) == false)
#endif
            {
                Console.WriteLine($"Impossible to find file '{filename}'");
                return false;
            }
            return true;
        }

        public static bool StringEquals(string s1, string s2)
        {
            return string.Equals(s1, s2, StringComparison.OrdinalIgnoreCase);
        }
    }
}
