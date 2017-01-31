using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aes.UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        private static readonly Regex Base64Regex = new Regex("^[A-Za-z0-9+/\r\n]+={0,2}$", RegexOptions.Compiled);

        [TestMethod]
        public void FileRaw_To_FileRaw_To_FileRaw()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            var program = new Program();

            string inputFilename = Utils.EnsureFilename("tests/clear.txt");
            string outputFilename = Utils.EnsureFilename("tests/encrypted.aes");

            string[] encryptArgs = new string[]
            {
                "-e",
                "--input-type", "file",
                "--input-format", "raw",
                "--output-type", "file",
                "--output-format", "raw",
                "--password", "abcd",
                "--key-salt-length", "64",
                "--key-iterations", "2000",
                "--key-size", "256",
                "--skip-bytes", "0",
                inputFilename,
                outputFilename
            };

            Directory.CreateDirectory(Utils.EnsureFilename("tests"));

            string clearText = "bob le furet en mousse";

            if (File.Exists(inputFilename))
                File.Delete(inputFilename);
            if (File.Exists(outputFilename))
                File.Delete(outputFilename);

            File.WriteAllText(inputFilename, clearText);

            program.Run(encryptArgs);

            Assert.IsTrue(File.Exists(outputFilename));
            Assert.AreEqual(2 + 16 + 2 + 64 + 32, new FileInfo(outputFilename).Length);

            inputFilename = outputFilename;
            outputFilename = Utils.EnsureFilename("tests/decrypted.txt");

            string[] decryptArgs = new string[]
            {
                "-d",
                "--input-type", "file",
                "--input-format", "raw",
                "--output-type", "file",
                "--output-format", "raw",
                "--password", "abcd",
                "--key-salt-length", "64",
                "--key-iterations", "2000",
                "--key-size", "256",
                "--skip-bytes", "0",
                inputFilename,
                outputFilename
            };

            program.Run(decryptArgs);

            Assert.IsTrue(File.Exists(outputFilename));
            Assert.AreEqual(clearText, File.ReadAllText(outputFilename));
        }

        [TestMethod]
        public void ConsoleRaw_To_ConsoleHex_To_ConsoleRaw()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            var program = new Program();

            string clearText = "bla bla useless and a bit longer than usual message lorem ipsum quick brown fox bla bla";

            string[] encryptArgs = new string[]
            {
                "-e",
                "--input-type", "console",
                "--input-format", "raw",
                "--output-type", "console",
                "--output-format", "hex",
                "--password", "abcd",
                "--key-salt-length", "64",
                "--key-iterations", "2000",
                "--key-size", "256",
                "--skip-bytes", "0",
                clearText
            };

            var sb = new StringBuilder();
            Console.SetOut(new StringWriter(sb));

            program.Run(encryptArgs);

            string[] decryptArgs = new string[]
            {
                "-d",
                "--input-type", "console",
                "--input-format", "hex",
                "--output-type", "console",
                "--output-format", "raw",
                "--password", "abcd",
                "--key-salt-length", "64",
                "--key-iterations", "2000",
                "--key-size", "256",
                "--skip-bytes", "0",
                ConsoleOutStream.TrimConsoleDataMarkers(sb.ToString())
            };

            sb = new StringBuilder();
            Console.SetOut(new StringWriter(sb));

            program.Run(decryptArgs);

            Assert.AreEqual(clearText, ConsoleOutStream.TrimConsoleDataMarkers(sb.ToString()));
        }

        [TestMethod]
        public void ConsoleRaw_To_ConsoleBase64_To_ConsoleRaw()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            var program = new Program();

            string clearText = "bla bla useless and a bit longer than usual message lorem ipsum quick brown fox bla bla";

            string[] encryptArgs = new string[]
            {
                "-e",
                "--input-type", "console",
                "--input-format", "raw",
                "--output-type", "console",
                "--output-format", "b64",
                "--password", "abcd",
                "--key-salt-length", "64",
                "--key-iterations", "2000",
                "--key-size", "256",
                "--skip-bytes", "0",
                clearText
            };

            var sb = new StringBuilder();
            Console.SetOut(new StringWriter(sb));

            program.Run(encryptArgs);

            string outputData = ConsoleOutStream.TrimConsoleDataMarkers(sb.ToString());

            Assert.IsTrue(Base64Regex.IsMatch(outputData));

            string[] decryptArgs = new string[]
            {
                "-d",
                "--input-type", "console",
                "--input-format", "b64",
                "--output-type", "console",
                "--output-format", "raw",
                "--password", "abcd",
                "--key-salt-length", "64",
                "--key-iterations", "2000",
                "--key-size", "256",
                "--skip-bytes", "0",
                outputData
            };

            sb = new StringBuilder();
            Console.SetOut(new StringWriter(sb));

            program.Run(decryptArgs);

            Assert.AreEqual(clearText, ConsoleOutStream.TrimConsoleDataMarkers(sb.ToString()));
        }

        [TestMethod]
        public void MinimalOptions()
        {
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);

            var program = new Program();

            string inputFilename = Utils.EnsureFilename("tests/clear.txt");
            string outputFilename = Utils.EnsureFilename("tests/encrypted.aes");

            string[] encryptArgs = new string[]
            {
                "-e",
                "--password", "abcd",
                inputFilename,
                outputFilename
            };

            Directory.CreateDirectory(Utils.EnsureFilename("tests"));

            string clearText = "This message is used to test encryption and decryption with minimal options. All optional ones are not provided in order to fallback to default values.";

            if (File.Exists(inputFilename))
                File.Delete(inputFilename);
            if (File.Exists(outputFilename))
                File.Delete(outputFilename);

            File.WriteAllText(inputFilename, clearText);

            program.Run(encryptArgs);

            Assert.IsTrue(File.Exists(outputFilename));

            inputFilename = outputFilename;
            outputFilename = Utils.EnsureFilename("tests/decrypted.txt");

            string[] decryptArgs = new string[]
            {
                "-d",
                "--password", "abcd",
                inputFilename,
                outputFilename
            };

            program.Run(decryptArgs);

            Assert.IsTrue(File.Exists(outputFilename));
            Assert.AreEqual(clearText, File.ReadAllText(outputFilename));
        }
    }
}
