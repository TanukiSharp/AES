using System;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.PlatformAbstractions;

namespace Aes
{
    public class CommandLine
    {
        public bool IsEncrypt { get; private set; }

        public DataType InputDataType { get; private set; } = DataType.None;
        public DataFormat InputDataFormat { get; private set; } = DataFormat.None;

        public DataType OutputDataType { get; private set; } = DataType.None;
        public DataFormat OutputDataFormat { get; private set; } = DataFormat.None;

        public int KeySaltLength { get; private set; }
        public int KeyIterations { get; private set; }
        public int KeySize { get; private set; }

        public string Password { get; private set; }

        public int SkipBytes { get; private set; }

        public string InputValue { get; private set; }
        public string OutputValue { get; private set; }

        // --------------------------------------------------------------------------

        private readonly bool hasParameters;
        private readonly CommandLineApplication commandLineApplication;

        private readonly CommandOption optEncrypt;
        private readonly CommandOption optDecrypt;

        private readonly CommandOption optInputType;
        private readonly CommandOption optOutputType;

        private readonly CommandOption optPassword;

        private readonly CommandOption optInputFormat;
        private readonly CommandOption optOutputFormat;

        private readonly CommandOption optKeySaltLength;
        private readonly CommandOption optKeyIterations;
        private readonly CommandOption optKeySize;

        private readonly CommandOption optSkipBytes;

        private readonly CommandArgument argInput;
        private readonly CommandArgument argOutput;

        public CommandLine(string[] args)
        {
            commandLineApplication = new CommandLineApplication
            {
                Name = "aes",
                FullName = "AES",
                Description = "An encryption and decryption tool based on the AES algorithm",
            };

            // -------------------------------------------------------------------------------

            optEncrypt = commandLineApplication.Option(
                "-e|--encrypt",
                "Encrypts the data",
                CommandOptionType.NoValue);

            optDecrypt = commandLineApplication.Option(
                "-d|--decrypt",
                "Decrypts the data",
                CommandOptionType.NoValue);

            // -------------------------------------------------------------------------------

            optInputType = commandLineApplication.Option(
                "--input-type",
                "The type of input source: console or file, defaults to file",
                CommandOptionType.SingleValue);

            optInputFormat = commandLineApplication.Option(
                "--input-format",
                "The format of input data: hex, base64 or raw, defaults to raw",
                CommandOptionType.SingleValue);

            // -------------------------------------------------------------------------------

            optOutputType = commandLineApplication.Option(
                "--output-type",
                "The type of output target: console or file, defaults to file",
                CommandOptionType.SingleValue);

            optOutputFormat = commandLineApplication.Option(
                "--output-format",
                "The format of ouput data: hex, base64 or raw, defaults to raw",
                CommandOptionType.SingleValue);

            // -------------------------------------------------------------------------------

            optPassword = commandLineApplication.Option(
                "--password",
                "The encryption or decryption password (NOT RECOMMENDED TO SET IT THAT WAY!)",
                CommandOptionType.SingleValue);

            // -------------------------------------------------------------------------------

            optKeySaltLength = commandLineApplication.Option(
                "--key-salt-length",
                $"The length of the salt to generate: integer between 8 and {ushort.MaxValue}, defaults to {Constants.DefaultSaltLength}",
                CommandOptionType.SingleValue);

            optKeyIterations = commandLineApplication.Option(
                "--key-iterations",
                $"The iterations used to prepare the key: must be greater than 0, defaults to {Constants.DefaultKeyIterations}",
                CommandOptionType.SingleValue);

            optKeySize = commandLineApplication.Option(
                "--key-size",
                $"Size of key blocks: 128, 192 or 256, defaults to {Constants.DefaultKeySize}",
                CommandOptionType.SingleValue);

            // -------------------------------------------------------------------------------

            optSkipBytes = commandLineApplication.Option(
                "--skip-bytes",
                $"Bytes to skip at the beginning of the data: defaults to {Constants.DefaultSkipBytes}",
                CommandOptionType.SingleValue);

            // -------------------------------------------------------------------------------

            argInput = commandLineApplication.Argument(
                "input",
                "The input text or filename",
                false);

            argOutput = commandLineApplication.Argument(
                "output",
                "The output text or filename",
                false);

            // -------------------------------------------------------------------------------

            string version = $"{PlatformServices.Default.Application.ApplicationVersion} build {GitCommitHash.Value}";
            commandLineApplication.VersionOption("--version", version, version);

            commandLineApplication.Execute(args);

            hasParameters = args.Length > 0;
        }

        public bool Execute()
        {
            if (hasParameters == false)
            {
                commandLineApplication.ShowHelp();
                return false;
            }

            if (ValidateInputOutputParameters() == false)
            {
                commandLineApplication.ShowHelp();
                return false;
            }

            if (ValidateCipherOptions() == false)
            {
                commandLineApplication.ShowHelp();
                return false;
            }

            if (ValidateExtraOptions() == false)
            {
                commandLineApplication.ShowHelp();
                return false;
            }

            return true;
        }

        private bool ValidateInputOutputParameters()
        {
            bool optEncryptHasValue = optEncrypt.HasValue();
            bool optDecryptHasValue = optDecrypt.HasValue();

            if (optEncryptHasValue == optDecryptHasValue)
            {
                if (optEncryptHasValue)
                    Console.WriteLine($"Options '{optEncrypt.Template}' and '{optDecrypt.Template}' are mutually exclusive.");
                else
                    Console.WriteLine($"Option '{optEncrypt.Template}' or '{optDecrypt.Template}' is mandatory.");

                return false;
            }
            else
                IsEncrypt = optEncryptHasValue;

            InputValue = argInput.Value;
            OutputValue = argOutput.Value;

            if (ValidateInputOptions() == false)
                return false;

            if (ValidateOutputOptions() == false)
                return false;

            if (optPassword.HasValue())
                Password = optPassword.Value();
            else
            {
                Console.Write("Enter password: ");
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Black;
                Password = Console.ReadLine();
                Console.ResetColor();
                Console.WriteLine();
            }

            return true;
        }

        private bool ValidateInputOptions()
        {
            bool optInputTypeHasValue = optInputType.HasValue();

            if (optInputTypeHasValue == false)
            {
                string filename = Utils.EnsureFilename(InputValue);
                if (Utils.CheckFile(filename) == false)
                    return false;
                else
                {
                    InputValue = filename;
                    InputDataType = DataType.File;
                }
            }
            else
            {
                string optInputTypeValue = optInputType.Value();

                if (Utils.StringEquals(optInputTypeValue, "file"))
                {
                    string filename = Utils.EnsureFilename(InputValue);
                    if (Utils.CheckFile(filename) == false)
                        return false;
                    else
                    {
                        InputValue = filename;
                        InputDataType = DataType.File;
                    }
                }
                else if (Utils.StringEquals(optInputTypeValue, "console"))
                    InputDataType = DataType.Console;
                else
                {
                    Console.WriteLine($"Invalid '{optInputType.Template}' option value, allowed: file, console");
                    return false;
                }
            }

            if (optInputFormat.HasValue())
            {
                if (Utils.TryGetFormat(optInputFormat.Value(), out DataFormat localDataFormat))
                    InputDataFormat = localDataFormat;
                else
                {
                    Console.WriteLine($"Invalid '{optInputFormat.Template}' option value, allowed: hex, base64, b64, raw");
                    return false;
                }
            }
            else
                InputDataFormat = DataFormat.Raw;

            return true;
        }

        private bool ValidateOutputOptions()
        {
            bool optOutputTypeHasValue = optOutputType.HasValue();

            if (optOutputTypeHasValue == false)
            {
                OutputValue = Utils.EnsureFilename(OutputValue);
                OutputDataType = DataType.File;
            }
            else
            {
                string optOutputTypeValue = optOutputType.Value();

                if (Utils.StringEquals(optOutputTypeValue, "file"))
                {
                    OutputValue = Utils.EnsureFilename(OutputValue);
                    OutputDataType = DataType.File;
                }
                else if (Utils.StringEquals(optOutputTypeValue, "console"))
                    OutputDataType = DataType.Console;
                else
                {
                    Console.WriteLine($"Invalid '{optOutputType.Template}' option value, allowed: file, console");
                    return false;
                }
            }

            if (optOutputFormat.HasValue())
            {
                if (Utils.TryGetFormat(optOutputFormat.Value(), out DataFormat localDataFormat))
                    OutputDataFormat = localDataFormat;
                else
                {
                    Console.WriteLine($"Invalid '{optOutputFormat.Template}' option value, allowed: hex, base64, b64, raw");
                    return false;
                }
            }
            else
                OutputDataFormat = DataFormat.Raw;

            return true;
        }

        private bool ValidateCipherOptions()
        {
            int keySize;
            int keyIterations;
            int keySaltLength;

            if (optKeySize.HasValue() == false)
                keySize = Constants.DefaultKeySize;
            else
            {
                string strKeySize = optKeySize.Value();

                if (strKeySize == "128" || strKeySize == "192" || strKeySize == "256")
                    keySize = int.Parse(strKeySize);
                else
                {
                    Console.WriteLine($"Invalid '{optKeySize.Template}' option value '{strKeySize}', allowed: 128, 192, 256.");
                    return false;
                }
            }

            if (optKeySaltLength.HasValue() == false)
                keySaltLength = Constants.DefaultSaltLength;
            else
            {
                if (int.TryParse(optKeySaltLength.Value(), out keySaltLength) == false || keySaltLength <= 0)
                {
                    Console.WriteLine($"Invalid '{optKeySaltLength.Template}' option value, allowed: positive integer.");
                    return false;
                }
            }

            if (optKeyIterations.HasValue() == false)
                keyIterations = Constants.DefaultKeyIterations;
            else
            {
                if (int.TryParse(optKeyIterations.Value(), out keyIterations) == false || keyIterations <= 0)
                {
                    Console.WriteLine($"Invalid '{optKeyIterations.Template}' option value '{optKeyIterations.Value()}'.");
                    return false;
                }
            }

            KeySize = keySize;
            KeyIterations = keyIterations;
            KeySaltLength = keySaltLength;

            return true;
        }

        private bool ValidateExtraOptions()
        {
            int skipBytes;

            if (optSkipBytes.HasValue() == false)
                skipBytes = Constants.DefaultSkipBytes;
            else
            {
                if (int.TryParse(optSkipBytes.Value(), out skipBytes) == false || skipBytes < 0)
                {
                    Console.WriteLine($"Invliad '{optSkipBytes.Template}' option value '{optSkipBytes.Value()}', allowed: positive integer value.");
                    return false;
                }
            }

            SkipBytes = skipBytes;

            return true;
        }
    }

    public enum DataType
    {
        None,
        Console,
        File
    }

    public enum DataFormat
    {
        None,
        Hexa,
        Base64,
        Raw
    }
}
