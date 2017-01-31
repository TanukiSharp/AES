# Overview

This application is a cross-platform tool to encrypt and decrypt data using the AES algorithm.

You can find information about this alogithm on the wikipedia page at https://en.wikipedia.org/wiki/Advanced_Encryption_Standard.

# How to build and run

You have to download and install .NET Core 1.1+.<br/>
You can find all the necessary instructions on the .NET Core page at https://www.microsoft.com/net/core.

Once this is done, you first need to restore the packages. To do so, open a console in the root folder and type the following command:

```
dotnet restore
```

Then go to the `Aes` directory, and build, typing the following commands:

```
cd Aes
dotnet build
```

Everything should be OK. If you encounter a problem, feel free to [file an issue](https://github.com/TanukiSharp/AES/issues/new) on this repository and I will answer as soon as I can.

Once this is done, go to the output directory in order to be able to execute the application with arguments:

```
cd bin/<configuration>/<runtime>
dotnet Aes.dll <parameters>
```

Where:
- `<configurartion>` is either `Debug` or `Release` (or else), depending on what you chose to build.<br/>
It is `Debug` by default, but you can choose to build a release version typing the `dotnet build -c release` command.<br/>
- `<runtime>` is the platform you ran `dotnet build` on.
- `<paramters>` are the options you provide to the application, and they are described in the next section.

**Note**: The casing you use for the words `debug` and `release` when building with th `-c` option does not matter.<br/>
However, it determines the name (and thus casing) of the configuration directory when created the first time, so becareful on OS with case sensitive file system.

# How to use it

```
dotnet Aes.dll <options> <input-argument> [<output-argument>]
```

## Options

### -e, --encrypt or -d, --decrypt

This option is mandatory, `-e` is short for `--encrypt` and `-d` is short for `--decrypt`.<br/>
Both encrypt and decrypt options are mutually exclusive.

To encrypt, either `-e` or `--encrypt` is necessary, but not both. Same with `-d` or `--decrypt` to decrypt data.

### --input-type console|file

This option is optional and can take two values, `file` or `console`. It defaults to `file` if not provided.

It indicates whether the input argument is a file path, with `--input-type file`, or if it is a raw value, with `--input-type console`.

### --input-format hex|b64|base64|raw

This option is optional and can take three values, `hex`, `base64` (`b64` for short) or `raw`. It defaults to `raw` if not provided.

It indicates whether the input data is either hexadecimal encoded, base64 encode, or not encoded at all, respectively.

If `hex` is provided, the input data is considered as ASCII characters containing only characters in the range `A` to `F`, `a` to `f` and `0` to `9`.<br/>
In this format, two characters result in one decoded byte.<br/>
For exemple, the hexadecimal value `48656C6C6F20576F726C6421` results in the value `Hello World!`.

If `base64` (or `b64`) is provided, the input data is considered as base64 to be decoded before being processed.<br/>
For exemple, the base64 value `SGVsbG8gV29ybGQh` results in the value `Hello World!`.

See the wikipedia page for more information about this encoding: https://en.wikipedia.org/wiki/Base64

If `raw` is provided, the input data is not decoded and processed as is, using the console input encoding to transform it to raw bytes data.

### --output-type console|file

This option is optional and can take two values, `file` or `console`. It defaults to `file` if not provided.

It indicates whether the output argument is a file path, with `--output-type file`, or if output data should be printed to the console, with `--output-type console`.<br/>
When the output type is set to `console`, the output argument is ignored.

### --output-format hex|b64|base64|raw

This option is optional and can take three values, `hex`, `base64` (`b64` for short) or `raw`. It defaults to `raw` if not provided.

It indicates whether the output data is written as hexadecimal encoded, base64 encoded, or not encoded at all, respectively.

If `hex` is provided, the raw output data is encodced to ASCII characters containing only characters in the range `a` to `f` and `0` to `9`.<br/>
In this format, one byte is encoded to two ASCII characters.<br/>
For exemple, the value `Hello World!` results in the hexadecimal value `48656C6C6F20576F726C6421`.

If `base64` (or `b64`) is provided, the raw output data is encoded to base64 before being written.<br/>
For exemple, the value `Hello World!` results in the base64 value `SGVsbG8gV29ybGQh`.

If `raw` is provided, the output data is not encoded at all and written as is, using the console output encoding to transform bytes to a printable string.

### --password ...

This option is optional, and is used to provide encryption or decryption password.

If not provided, the application prompts for password to be typed directly in the console. When entered this way, the characters are hidden.<br/>
It is recommended to **not** use the `--password <password>` option, or at least to not use it in scripts stored on disk.

### --key-salt-length ...

This option is optional, it takes an integer value between 8 and 65'535. It defaults to `32` if not provided.<br/>
This is used only when encrypting, to generate a random salt of the provided amount of bytes.<br/>
When decrypting, the salt is read from the input data.

See this wikipedia page https://en.wikipedia.org/wiki/PBKDF2 or the RFC 2898 https://tools.ietf.org/html/rfc2898 for more informaton.

### --key-iterations ...

This option is optional, it takes an integer value greater than 0. It defaults to `2000` if not provided.<br/>
The minimum recommended value is `1000`.

See this wikipedia page https://en.wikipedia.org/wiki/PBKDF2 or the RFC 2898 https://tools.ietf.org/html/rfc2898 for more informaton.

### --key-size 128|192|256

This option is optional and can take three values, `128`, `192` or `256`. It defaults to `256` if not provided.<br/>
This sets the size of key blocks used by the AES algorithm.

See the wikipedia page of the AES algorithm for more information: https://en.wikipedia.org/wiki/Advanced_Encryption_Standard

### --skip-bytes ...

This option is optional, it takes an integer value greater than or equal to 0. It defaults to `0` if not provided.<br/>
This is used to skip the first bytes of input data, in case there would be a header to discard before the payload.

This option is used only when decrypting and is ignored when encrypting.

## Arguments

### Input argument

This argument is mandatory.

It is either the path of the file containing the input data to encrypt or decrypt, or the input data provided through the command line.
(see `--input-type` option for more information)

If file path is provided, it can be either absolute or not. If it is not absolute, it is relative to the current working directory.<br/>
The input file must exist or the process will fail.

### Output argument

This argument is mandatory when output type is set to `file`, and should be ommited when output type is set to `console`.<br/>
If a value is provided when in `console` output type, the value is simply ignored.
(see `--output-type` for more information)

When output is a file, this is the path of the file where to store the output data.<br/>
The path can be either absolute or not. If it is not absolute, it is relative to the current working directory.<br/>
The output file does not have to exist for the process to run. If the output file already exists, it is overwritten.

## Default settings

### Padding

The padding used in tool is hard-coded to `PKCS7`.<br/>
This sets the way data is padded when there is not enough to fulfill a cipher block.

For more informarion, see the wikipedia page of the AES algorithm https://en.wikipedia.org/wiki/Advanced_Encryption_Standard or the page about padding https://en.wikipedia.org/wiki/Padding_(cryptography) for more information.

### Cipher mode

The cipher mode used in this application is `CBC`.<br/>
This sets the way an encrypted block impacts the next one, and so on.

See the wikipedia page of the cipher block modes for more information: https://en.wikipedia.org/wiki/Block_cipher_mode_of_operation

# Encrypted data format

```
+---------+-----------------------+------------------+-----------...------------+-------------...---------------+
| 2 bytes |       16 bytes        |     2 bytes      | <salt-bytes-count> bytes | <remaining-bytes-count> bytes |
+---------+-----------------------+------------------+-----------...------------+-------------...---------------+
| version | initialization vector | salt bytes count | salt                     | encrypted data                |
+---------+-----------------------+------------------+-----------...------------+-------------...---------------+
                                           \                      ^
                                            ---------------------/
```

The width of the cells on the table above are not at scale.

To summarize the format in a more verbose and textual manner, the file starts with 2 bytes of version, then 16 bytes of initialization vector (IV), then 2 bytes indicating the size of the next block of data, which is the amount of bytes of salt.<br/>
The version and the salt bytes count are always unsigned and written in network order. Then comes the salt itself, of length given by the 2 previous bytes, then comes the encrypted data, which runs until the end of the whole data.

The initialization vector (IV) is always 16 bytes long (128 bits) because it has to match the cipher block size, which is always 128 bits with the AES algorithm.

The salt is of variable length because it is up to the encrypter to set it.

**Note:** The AES alogithm allows 128, 192 or 256 bits (respectively 16, 24 or 32 bytes) of key block size, but has a fixed cipher block size of 128 bits (16 bytes).

# Tests

There are only 4 unit tests that run the most basic scenarios.

Additionally, there is a testing application that tries all the possible combination of options, resulting in 194'400 tests.<br/>
Some situations are skipped because they do not make sense, such as displaying raw encrypted data to the console.

```
Test passed: 194400 / 194400 (100.00 %)
Effective tests: 135000 / 194400 (69.44 %)
Skipped tests:   59400 (30.56 %)
```

Note that the 135'000 tests reading and writing to disk may not be what you want to do, so in order to avoid stressing the disk, an in-memory fallback is implemented.<br/>
It is activated in debug build configurarion only, not in release. It is necessary to disable it to run the unit tests, so you have to manually get rid of the `IN_MEMORY` conditional compilation symbol.
