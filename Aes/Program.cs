#if DEBUG
#define IN_MEMORY
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using crypto = System.Security.Cryptography;

[assembly: InternalsVisibleTo("Aes.UnitTests")]
[assembly: InternalsVisibleTo("Aes.TestApp")]

namespace Aes
{
    public class Program
    {
        private static int Main(string[] args)
        {
            //args = new string[] { "-e", "--input-type", "file", "--input-format", "raw", "tests\\input.txt", "--output-type", "file", "--output-format", "raw", "tests\\output.aes", "--password", "abcd" };
            //args = new string[] { "-d", "--input-type", "file", "--input-format", "raw", "tests\\output.aes", "--output-type", "file", "--output-format", "raw", "tests\\input_decrypted.txt", "--password", "abcd" };

            return new Program().Run(args);
        }

        private List<Stream> inputStreams = new List<Stream>();
        private List<Stream> outputStreams = new List<Stream>();

        internal int Run(string[] args)
        {
            var commandLine = new CommandLine(args);

            if (commandLine.Execute() == false)
                return 1;

            crypto.Aes cipher;

            try
            {
                cipher = crypto.Aes.Create();

                cipher.KeySize = commandLine.KeySize;
                cipher.Mode = Constants.DefaultCipherMode;
                cipher.Padding = Constants.DefaultPadding;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }

            try
            {
                if (commandLine.IsEncrypt)
                    ProcessEncryption(cipher, commandLine);
                else
                    return ProcessDecryption(cipher, commandLine);
            }
            catch (CryptographicException ex)
            {
                Console.WriteLine("Invalid cryptographic operation.");
                Console.WriteLine($"Error message: {ex.Message}");
            }
            finally
            {
                foreach (Stream stream in inputStreams.Where(x => x != null))
                    stream.Dispose();
                foreach (Stream stream in outputStreams.Where(x => x != null).Reverse())
                    stream.Dispose();
            }

            return 0;
        }

        private Stream UseStream(bool isInput, Stream stream)
        {
            if (isInput)
                inputStreams.Add(stream);
            else
                outputStreams.Add(stream);
            return stream;
        }

        internal Stream CreateInputStream(CommandLine commandLine)
        {
            Stream inputStream;

            if (commandLine.InputDataType == DataType.File)
            {
#if IN_MEMORY
                inputStream = UseStream(true, new InMemoryFileStream(commandLine.InputValue, FileMode.Open, FileAccess.Read));
#else
                inputStream = UseStream(true, new FileStream(commandLine.InputValue, FileMode.Open, FileAccess.Read));
#endif
            }
            else if (commandLine.InputDataType == DataType.Console)
                inputStream = UseStream(true, new MemoryStream(Console.InputEncoding.GetBytes(commandLine.InputValue)));
            else
                throw new InvalidOperationException();

            if (commandLine.InputDataFormat == DataFormat.Raw)
                return inputStream;
            else if (commandLine.InputDataFormat == DataFormat.Hexa)
                return UseStream(true, new HexadecimalStream(inputStream));
            else if (commandLine.InputDataFormat == DataFormat.Base64)
            {
                // TODO: replace by new CryptoStream(new FromBase64Transform()); when .NET Standard 2.0 will be released
                string b64;
                using (var sr = new StreamReader(inputStream))
                    b64 = sr.ReadToEnd();
                return UseStream(true, new MemoryStream(Convert.FromBase64String(b64)));
            }
            else
                throw new InvalidOperationException();
        }

        internal Stream CreateOutputStream(CommandLine commandLine)
        {
            Stream outputStream;

            if (commandLine.OutputDataType == DataType.File)
            {
#if IN_MEMORY
                outputStream = UseStream(false, new InMemoryFileStream(commandLine.OutputValue, FileMode.Create, FileAccess.Write));
#else
                outputStream = UseStream(false, new FileStream(commandLine.OutputValue, FileMode.Create, FileAccess.Write));
#endif
            }
            else if (commandLine.OutputDataType == DataType.Console)
                outputStream = UseStream(false, new ConsoleOutStream(Console.OutputEncoding));
            else
                throw new InvalidOperationException();

            if (commandLine.OutputDataFormat == DataFormat.Raw)
                return outputStream;
            else if (commandLine.OutputDataFormat == DataFormat.Hexa)
                return UseStream(false, new HexadecimalStream(outputStream));
            else if (commandLine.OutputDataFormat == DataFormat.Base64)
            {
                // TODO: replace by new CryptoStream(new ToBase64Transform()); when .NET Standard 2.0 will be released
                return UseStream(false, new TemporaryBase64OutStream(outputStream));
            }
            else
                throw new InvalidOperationException();
        }

        internal void ProcessEncryption(crypto.Aes cipher, CommandLine commandLine)
        {
            var key = new Rfc2898DeriveBytes(commandLine.Password, commandLine.KeySaltLength, commandLine.KeyIterations);

            cipher.GenerateIV();
            cipher.Key = key.GetBytes(cipher.KeySize / 8);

            Stream inputStream = CreateInputStream(commandLine);
            Stream outputStream = CreateOutputStream(commandLine);

            // 2 bytes of version
            outputStream.Write(Constants.FormatVersionBytes, 0, Constants.FormatVersionBytes.Length);

            // stores 16 bytes of IV
            outputStream.Write(cipher.IV, 0, cipher.IV.Length);

            // stores 2 bytes of salt size (in network order)
            short saltLength = IPAddress.HostToNetworkOrder((short)key.Salt.Length);
            byte[] saltLengthBytes = BitConverter.GetBytes(saltLength);
            outputStream.Write(saltLengthBytes, 0, saltLengthBytes.Length);

            // stores n bytes of salt
            outputStream.Write(key.Salt, 0, key.Salt.Length);

            using (ICryptoTransform encryptor = cipher.CreateEncryptor())
            {
                using (Stream cryptoStream = new CryptoStream(outputStream, encryptor, CryptoStreamMode.Write))
                    inputStream.CopyTo(cryptoStream);
            }
        }

        internal int ProcessDecryption(crypto.Aes cipher, CommandLine commandLine)
        {
            Stream inputStream = CreateInputStream(commandLine);
            Stream outputStream = CreateOutputStream(commandLine);

            if (commandLine.SkipBytes > 0)
            {
                if (commandLine.SkipBytes >= inputStream.Length)
                    return 0;

                if (inputStream.CanSeek)
                    inputStream.Position = commandLine.SkipBytes;
                else
                {
                    for (int i = 0; i < commandLine.SkipBytes; i++)
                        inputStream.ReadByte();
                }
            }

            var versionBytes = new byte[2];
            inputStream.Read(versionBytes, 0, versionBytes.Length);
            ushort version = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(versionBytes, 0));

            if (version != Constants.FormatVersion)
            {
                Console.WriteLine($"Unsupported version, expected {Constants.FormatVersion}, found {version}.");
                return 1;
            }

            // reads 16 bytes of IV
            var iv = new byte[cipher.BlockSize / 8]; // cipher.BlockSize / 8 = 16 since block size is always 128 for AES algorithm
            inputStream.Read(iv, 0, iv.Length);

            // reads 2 bytes of salt size (in host order)
            byte[] saltLengthBytes = new byte[sizeof(ushort)];
            inputStream.Read(saltLengthBytes, 0, saltLengthBytes.Length);
            int saltLength = (ushort)IPAddress.NetworkToHostOrder(BitConverter.ToInt16(saltLengthBytes, 0));

            // reads n bytes of salt
            byte[] salt = new byte[saltLength];
            inputStream.Read(salt, 0, salt.Length);

            var key = new Rfc2898DeriveBytes(commandLine.Password, salt, commandLine.KeyIterations);

            cipher.IV = iv;
            cipher.Key = key.GetBytes(cipher.KeySize / 8);

            using (Stream cryptoStream = new CryptoStream(inputStream, cipher.CreateDecryptor(), CryptoStreamMode.Read))
                cryptoStream.CopyTo(outputStream);

            return 0;
        }
    }
}
