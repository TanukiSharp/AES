#define IN_MEMORY

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aes;

class Program
{
    static void Main(string[] args)
    {
        TextWriter originalConsoleOut = Console.Out;

        DataType[][] dataTypeCombinations = GenerateCombination(3, DataType.Console, DataType.File);
        DataFormat[][] dataFormatCombinations = GenerateCombination(3, DataFormat.Hexa, DataFormat.Base64, DataFormat.Raw);

        // File Console Console, Base64 Base64 Raw
        //DataType[][] dataTypeCombinations = { new DataType[] { DataType.File, DataType.Console, DataType.Console } };
        //DataFormat[][] dataFormatCombinations = { new DataFormat[] { DataFormat.Base64, DataFormat.Base64, DataFormat.Raw } };

        //DataFormat[][] dataFormatCombinations =
        //{
        //    new DataFormat[] { DataFormat.Base64, DataFormat.Raw, DataFormat.Base64 }
        //};

        string[] passwords =
        {
            Encoding.ASCII.GetString(GenerateRandom(6, 13, true)), // short
            Encoding.ASCII.GetString(GenerateRandom(12, 19, true)), // medium
            Encoding.ASCII.GetString(GenerateRandom(19, 33, true)), // long
        };

        int[] saltLengths = { 32, 48, 64, 96, 128 };
        int[] keyIterations = { 500, 1000, 1500, 2000, 2500 };

        int[] keySizes = { 128, 192, 256 };

        int[] messageLengths =
        {
            random.Next(15, 36),
            random.Next(55, 136),
            random.Next(256, 1025),
            4096
        };

        var outputPath = Utils.EnsureFilename("tests");
        if (Directory.Exists(outputPath) == false)
            Directory.CreateDirectory(outputPath);

        int testCount = 0;
        int effectiveTestCount = 0;

        int totalTestCount =
            dataTypeCombinations.Length *
            dataFormatCombinations.Length *
            passwords.Length *
            saltLengths.Length *
            keyIterations.Length *
            keySizes.Length *
            messageLengths.Length;

        DateTime start = DateTime.Now;
        Console.WriteLine($"Tests started at {start}");
        Console.WriteLine();

        foreach (var dataTypeCombination in dataTypeCombinations)
        {
            foreach (var dataFormatCombination in dataFormatCombinations)
            {
                foreach (var password in passwords)
                {
                    foreach (var saltLength in saltLengths)
                    {
                        foreach (var keyIteration in keyIterations)
                        {
                            foreach (var keySize in keySizes)
                            {
                                foreach (var messageLength in messageLengths)
                                {
                                    bool isEffective = EncryptDecrypt(
                                        dataTypeCombination[0], dataTypeCombination[1], dataTypeCombination[2],
                                        dataFormatCombination[0], dataFormatCombination[1], dataFormatCombination[2],
                                        password, saltLength, keyIteration, keySize, messageLength
                                    );

                                    if (isEffective)
                                        effectiveTestCount++;

                                    Console.SetOut(originalConsoleOut);

                                    testCount++;
                                    Console.Write($"\rTest passed: {testCount} / {totalTestCount} ({(testCount * 100.0f / totalTestCount):f2} %)");
                                }
                            }
                        }
                    }
                }
            }
        }

        Console.WriteLine($"\rTest passed: {testCount} / {totalTestCount} ({(testCount * 100.0f / totalTestCount):f2} %)");
        Console.WriteLine($"Effective tests: {effectiveTestCount} / {totalTestCount} ({(effectiveTestCount * 100.0f / totalTestCount):f2} %)");
        Console.WriteLine($"Skipped tests:   {(totalTestCount - effectiveTestCount)} ({((totalTestCount - effectiveTestCount) * 100.0f / totalTestCount):f2} %)");

        Console.WriteLine();

        DateTime end = DateTime.Now;

        Console.WriteLine($"Tests ended at {end} (duration: {end - start})");
    }

    private static bool EncryptDecrypt(
        DataType startType, DataType interType, DataType endType,
        DataFormat startFormat, DataFormat interFormat, DataFormat endFormat,
        string password,
        int saltLength, int keyIterations, int keySize,
        int messageLength
    )
    {
        if ((interType == DataType.Console && interFormat == DataFormat.Raw) || (endType == DataType.Console && endFormat == DataFormat.Raw))
            return false;

        string startInputArg;
        string startOutputArg = string.Empty;

        byte[] originalClearMessage;

        if (startType == DataType.Console)
        {
            do
            {
                originalClearMessage = GenerateRandom(messageLength, true);

                if (startFormat == DataFormat.Base64)
                    startInputArg = Convert.ToBase64String(originalClearMessage);
                else if (startFormat == DataFormat.Hexa)
                    startInputArg = ToHexString(originalClearMessage);
                else
                    startInputArg = Encoding.ASCII.GetString(originalClearMessage);
            } while (startInputArg.StartsWith("-"));
        }
        else
        {
            originalClearMessage = GenerateRandom(messageLength, false);
            string startFilename = Utils.EnsureFilename("tests/clear.bin");
#if IN_MEMORY
            if (InMemoryFile.Exists(startFilename))
                InMemoryFile.Delete(startFilename);

            if (startFormat == DataFormat.Base64)
                InMemoryFile.WriteAllText(startFilename, Convert.ToBase64String(originalClearMessage));
            else if (startFormat == DataFormat.Hexa)
                InMemoryFile.WriteAllText(startFilename, ToHexString(originalClearMessage));
            else
                InMemoryFile.WriteAllBytes(startFilename, originalClearMessage);
#else
            if (File.Exists(startFilename))
                File.Delete(startFilename);
            if (startFormat == DataFormat.Base64)
                File.WriteAllText(startFilename, Convert.ToBase64String(originalClearMessage));
            else if (startFormat == DataFormat.Hexa)
                File.WriteAllText(startFilename, ToHexString(originalClearMessage));
            else
                File.WriteAllBytes(startFilename, originalClearMessage);
#endif
            startInputArg = startFilename;
        }

        StringBuilder interStringBuilder = null;

        if (interType == DataType.Console)
        {
            interStringBuilder = new StringBuilder();
            Console.SetOut(new StringWriter(interStringBuilder));
        }
        else
        {
            string interFilename = Utils.EnsureFilename("tests/encrypted.aes");
#if IN_MEMORY
            if (InMemoryFile.Exists(interFilename))
                InMemoryFile.Delete(interFilename);
#else
            if (File.Exists(interFilename))
                File.Delete(interFilename);
#endif
            startOutputArg = interFilename;
        }

        var startArgs = new string[]
        {
            "-e",
            "--input-type", startType.ToString().ToLowerInvariant(),
            "--input-format", startFormat.ToString().ToUpperInvariant(),
            "--output-type", interType.ToString().ToLowerInvariant(),
            "--output-format", interFormat.ToString().ToLowerInvariant(),
            "--password", password,
            "--key-salt-length", saltLength.ToString(),
            "--key-iterations", keyIterations.ToString(),
            "--key-size", keySize.ToString(),
            "--skip-bytes", "0",
            startInputArg,
            startOutputArg
        };

        new Aes.Program().Run(startArgs);

        // ==================================================

        string endInputArg;
        string endOutputArg = string.Empty;

        if (interType == DataType.Console)
            endInputArg = ConsoleOutStream.TrimConsoleDataMarkers(interStringBuilder.ToString());
        else
            endInputArg = Utils.EnsureFilename("tests/encrypted.aes");

        StringBuilder endStringBuilder = null;

        if (endType == DataType.Console)
        {
            endStringBuilder = new StringBuilder();
            Console.SetOut(new StringWriter(endStringBuilder));
        }
        else
        {
            string endFilename = Utils.EnsureFilename("tests/decrypted.bin");
#if IN_MEMORY
            if (InMemoryFile.Exists(endFilename))
                InMemoryFile.Delete(endFilename);
#else
            if (File.Exists(endFilename))
                File.Delete(endFilename);
#endif
            endOutputArg = endFilename;
        }

        var endArgs = new string[]
        {
            "-d",
            "--input-type", interType.ToString().ToLowerInvariant(),
            "--input-format", interFormat.ToString().ToUpperInvariant(),
            "--output-type", endType.ToString().ToLowerInvariant(),
            "--output-format", endFormat.ToString().ToLowerInvariant(),
            "--password", password,
            "--key-salt-length", saltLength.ToString(),
            "--key-iterations", keyIterations.ToString(),
            "--key-size", keySize.ToString(),
            "--skip-bytes", "0",
            endInputArg,
            endOutputArg
        };

        new Aes.Program().Run(endArgs);

        byte[] resultMessage = null;

        if (endType == DataType.Console)
        {
            resultMessage = Encoding.ASCII.GetBytes(ConsoleOutStream.TrimConsoleDataMarkers(endStringBuilder.ToString()));
        }
        else
        {
#if IN_MEMORY
            resultMessage = InMemoryFile.ReadAllBytes(Utils.EnsureFilename("tests/decrypted.bin"));
#else
            resultMessage = File.ReadAllBytes(Utils.EnsureFilename("tests/decrypted.bin"));
#endif
        }

        if (endFormat == DataFormat.Base64)
            resultMessage = Convert.FromBase64String(Encoding.ASCII.GetString(resultMessage));
        else if (endFormat == DataFormat.Hexa)
            resultMessage = FromHexStringAsBytes(resultMessage);

        string info = $"{startType} {interType} {endType}, {startFormat} {interFormat} {endFormat}, {password}, {saltLength}, {keyIterations}, {keySize}, {messageLength}";

        if (originalClearMessage.Length != resultMessage.Length)
            throw new Exception($"Length mismatch. [{info}]");

        for (int i = 0; i < originalClearMessage.Length; i++)
        {
            if (originalClearMessage[i] != resultMessage[i])
                throw new Exception($"Data mismatch at position '{i}'. [{info}]");
        }

        return true;
    }

    private static readonly Random random = new Random(Guid.NewGuid().GetHashCode());

    private static string ToHexString(byte[] data)
    {
        var output = new StringBuilder();

        for (int i = 0; i < data.Length; i++)
        {
            output.Append((char)Utils.ByteToHexChar(data[i] >> 4));
            output.Append((char)Utils.ByteToHexChar(data[i] & 0xF));
        }

        return output.ToString();
    }

    private static byte[] FromHexStringAsBytes(byte[] str)
    {
        var output = new byte[str.Length / 2];

        for (int i = 0; i < output.Length; i++)
            output[i] = (byte)(Utils.HexCharToByte(str[i * 2]) << 4 | Utils.HexCharToByte(str[i * 2 + 1]));

        return output;
    }

    private static byte[] GenerateRandom(int count, bool isPrintable)
    {
        return GenerateRandom(count, count, isPrintable);
    }

    private static byte[] GenerateRandom(int minCount, int maxCount, bool isPrintable)
    {
        var output = new byte[minCount == maxCount ? minCount : random.Next(minCount, maxCount)];

        if (isPrintable)
        {
            for (int i = 0; i < output.Length; i++)
                output[i] = (byte)random.Next(32, 127);
        }
        else
            random.NextBytes(output);

        return output;
    }

    private static T[][] GenerateCombination<T>(int count, params T[] values)
    {
        var result = new List<T[]>();

        var indices = new int[count];

        while (true)
        {
            var current = new T[indices.Length];

            for (int j = 0; j < current.Length; j++)
                current[j] = values[indices[j]];

            result.Add(current);

            bool isDone = false;

            for (int i = indices.Length - 1; i >= 0; i--)
            {
                indices[i]++;
                if (indices[i] >= values.Length)
                {
                    if (i == 0)
                    {
                        isDone = true;
                        break;
                    }
                    else
                        indices[i] = 0;
                }
                else
                    break;
            }

            if (isDone)
                break;
        }

        return result.ToArray();
    }
}