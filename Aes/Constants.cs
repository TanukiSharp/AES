using System;
using System.Net;
using System.Security.Cryptography;

namespace Aes
{
    public static class Constants
    {
        public const ushort FormatVersion = 1;
        public static readonly byte[] FormatVersionBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder((short)FormatVersion));

        public const int DefaultKeySize = 256; // in bits

        public const int DefaultSaltLength = 32; // in bytes
        public const int DefaultKeyIterations = 2000;

        public const int DefaultSkipBytes = 0;

        public const CipherMode DefaultCipherMode = CipherMode.CBC;
        public const PaddingMode DefaultPadding = PaddingMode.PKCS7;
    }
}
