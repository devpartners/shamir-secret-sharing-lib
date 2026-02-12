using System.Text;
using ShamirSecretSharing.Core;

return CliApplication.Run(args);

internal static class CliApplication
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static int Run(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                return RunInteractive();
            }

            var command = args[0].Trim().ToLowerInvariant();
            var commandArgs = args.Skip(1).ToArray();

            return command switch
            {
                "split" => RunSplitCommand(commandArgs, allowInteractiveFallback: true),
                "combine" => RunCombineCommand(commandArgs, allowInteractiveFallback: true),
                "--help" or "-h" or "help" => ShowUsageAndReturn(),
                _ => UnknownCommand(command)
            };
        }
        catch (ShamirValidationException ex)
        {
            WriteError(ex.Message);
            return 2;
        }
        catch (ShamirReconstructionException ex)
        {
            WriteError(ex.Message);
            return 2;
        }
        catch (ArgumentException ex)
        {
            WriteError(ex.Message);
            return 2;
        }
        catch (Exception ex)
        {
            WriteError($"Unexpected error: {ex.Message}");
            return 1;
        }
    }

    private static int RunInteractive()
    {
        Console.Error.Write("Mode [split/combine]: ");
        var mode = Console.ReadLine()?.Trim().ToLowerInvariant();

        return mode switch
        {
            "split" => RunSplitCommand(Array.Empty<string>(), allowInteractiveFallback: true),
            "combine" => RunCombineCommand(Array.Empty<string>(), allowInteractiveFallback: true),
            _ => throw new ArgumentException("Interactive mode requires selecting either 'split' or 'combine'.")
        };
    }

    private static int RunSplitCommand(string[] args, bool allowInteractiveFallback)
    {
        var options = ParseSplitOptions(args);
        if (allowInteractiveFallback && IsSplitIncomplete(options))
        {
            options = PromptForSplitOptions(options);
        }

        ValidateSplitOptions(options);

        var secretBytes = ResolveSecretBytes(options);
        var shares = ShamirSecretSharer.Split(secretBytes, options.ShareCount!.Value, options.Threshold!.Value);
        var lines = shares.Select(ShareJsonCodec.Serialize).ToArray();

        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }

        if (!string.IsNullOrWhiteSpace(options.OutputPath))
        {
            File.WriteAllLines(options.OutputPath, lines);
            Console.Error.WriteLine($"Wrote {lines.Length} share(s) to {options.OutputPath}.");
        }

        return 0;
    }

    private static int RunCombineCommand(string[] args, bool allowInteractiveFallback)
    {
        var options = ParseCombineOptions(args);
        if (allowInteractiveFallback && IsCombineIncomplete(options))
        {
            options = PromptForCombineOptions(options);
        }

        ValidateCombineOptions(options);

        var shares = LoadShares(options);
        var secret = ShamirSecretSharer.Combine(shares);

        if (options.AsText)
        {
            try
            {
                Console.WriteLine(StrictUtf8.GetString(secret));
            }
            catch (DecoderFallbackException ex)
            {
                throw new ArgumentException("Reconstructed secret is not valid UTF-8 text. Omit --as-text to emit base64.", ex);
            }
        }
        else
        {
            Console.WriteLine(Convert.ToBase64String(secret));
        }

        return 0;
    }

    private static SplitOptions ParseSplitOptions(string[] args)
    {
        var options = new SplitOptions();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--shares":
                    options.ShareCount = ParseInt(ReadRequiredValue(args, ref i, "--shares"), "--shares");
                    break;
                case "--threshold":
                    options.Threshold = ParseInt(ReadRequiredValue(args, ref i, "--threshold"), "--threshold");
                    break;
                case "--secret-text":
                    options.SecretText = ReadRequiredValue(args, ref i, "--secret-text");
                    break;
                case "--secret-base64":
                    options.SecretBase64 = ReadRequiredValue(args, ref i, "--secret-base64");
                    break;
                case "--out":
                    options.OutputPath = ReadRequiredValue(args, ref i, "--out");
                    break;
                default:
                    throw new ArgumentException($"Unknown split option: {args[i]}");
            }
        }

        return options;
    }

    private static CombineOptions ParseCombineOptions(string[] args)
    {
        var options = new CombineOptions();

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--share-json":
                    options.ShareJson.Add(ReadRequiredValue(args, ref i, "--share-json"));
                    break;
                case "--shares-file":
                    options.SharesFile = ReadRequiredValue(args, ref i, "--shares-file");
                    break;
                case "--as-text":
                    options.AsText = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown combine option: {args[i]}");
            }
        }

        return options;
    }

    private static SplitOptions PromptForSplitOptions(SplitOptions current)
    {
        var options = current with { };

        options.ShareCount ??= PromptInt("Number of shares (2-256): ", min: 2, max: 256);
        options.Threshold ??= PromptInt($"Threshold (2-{options.ShareCount.Value}): ", min: 2, max: options.ShareCount.Value);

        if (string.IsNullOrWhiteSpace(options.SecretText) && string.IsNullOrWhiteSpace(options.SecretBase64))
        {
            var inputType = PromptChoice("Secret input type [text/base64] (default text): ", "text", "base64", "text");
            if (inputType == "base64")
            {
                options.SecretBase64 = PromptRequired("Secret (base64): ");
            }
            else
            {
                options.SecretText = PromptRequired("Secret (text): ");
            }
        }

        if (string.IsNullOrWhiteSpace(options.OutputPath))
        {
            Console.Error.Write("Output file path (optional): ");
            var outputPath = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(outputPath))
            {
                options.OutputPath = outputPath;
            }
        }

        return options;
    }

    private static CombineOptions PromptForCombineOptions(CombineOptions current)
    {
        var options = current with { };

        if (IsCombineIncomplete(options))
        {
            Console.Error.Write("Shares file path (leave blank to paste JSON lines): ");
            var sharesFile = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(sharesFile))
            {
                options.SharesFile = sharesFile;
            }
            else
            {
                Console.Error.WriteLine("Paste share JSON lines. Enter an empty line to finish.");
                while (true)
                {
                    var line = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        break;
                    }

                    options.ShareJson.Add(line);
                }
            }
        }

        if (!options.AsText)
        {
            var output = PromptChoice("Output format [base64/text] (default base64): ", "base64", "text", "base64");
            options.AsText = output == "text";
        }

        return options;
    }

    private static void ValidateSplitOptions(SplitOptions options)
    {
        if (!options.ShareCount.HasValue)
        {
            throw new ArgumentException("Missing required option --shares.");
        }

        if (!options.Threshold.HasValue)
        {
            throw new ArgumentException("Missing required option --threshold.");
        }

        if (!string.IsNullOrWhiteSpace(options.SecretText) && !string.IsNullOrWhiteSpace(options.SecretBase64))
        {
            throw new ArgumentException("Provide exactly one secret input: --secret-text or --secret-base64.");
        }

        if (string.IsNullOrWhiteSpace(options.SecretText) && string.IsNullOrWhiteSpace(options.SecretBase64))
        {
            throw new ArgumentException("Provide one secret input: --secret-text or --secret-base64.");
        }
    }

    private static void ValidateCombineOptions(CombineOptions options)
    {
        if (IsCombineIncomplete(options))
        {
            throw new ArgumentException("Provide shares through --share-json and/or --shares-file.");
        }
    }

    private static bool IsSplitIncomplete(SplitOptions options)
    {
        return !options.ShareCount.HasValue
            || !options.Threshold.HasValue
            || (string.IsNullOrWhiteSpace(options.SecretText) && string.IsNullOrWhiteSpace(options.SecretBase64));
    }

    private static bool IsCombineIncomplete(CombineOptions options)
    {
        return options.ShareJson.Count == 0 && string.IsNullOrWhiteSpace(options.SharesFile);
    }

    private static byte[] ResolveSecretBytes(SplitOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SecretText))
        {
            var bytes = Encoding.UTF8.GetBytes(options.SecretText);
            if (bytes.Length == 0)
            {
                throw new ArgumentException("Secret text must not be empty.");
            }

            return bytes;
        }

        try
        {
            var bytes = Convert.FromBase64String(options.SecretBase64!);
            if (bytes.Length == 0)
            {
                throw new ArgumentException("Secret base64 must decode to at least one byte.");
            }

            return bytes;
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("The value for --secret-base64 is not valid base64.", ex);
        }
    }

    private static IReadOnlyList<ShamirShare> LoadShares(CombineOptions options)
    {
        var shares = new List<ShamirShare>();

        foreach (var json in options.ShareJson)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                continue;
            }

            shares.Add(ShareJsonCodec.Deserialize(json));
        }

        if (!string.IsNullOrWhiteSpace(options.SharesFile))
        {
            if (!File.Exists(options.SharesFile))
            {
                throw new ArgumentException($"Shares file does not exist: {options.SharesFile}");
            }

            shares.AddRange(ShareJsonCodec.DeserializeMany(File.ReadLines(options.SharesFile)));
        }

        if (shares.Count == 0)
        {
            throw new ArgumentException("No shares were loaded from the provided inputs.");
        }

        return shares;
    }

    private static int ShowUsageAndReturn()
    {
        Console.Error.WriteLine("Shamir Secret Sharing CLI");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Split:");
        Console.Error.WriteLine("  split --shares <n> --threshold <t> [--secret-text <value> | --secret-base64 <value>] [--out <path>]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Combine:");
        Console.Error.WriteLine("  combine [--share-json <json>]... [--shares-file <path>] [--as-text]");
        Console.Error.WriteLine();
        Console.Error.WriteLine("If required arguments are omitted, the CLI prompts interactively.");
        return 0;
    }

    private static int UnknownCommand(string command)
    {
        WriteError($"Unknown command '{command}'. Use 'split', 'combine', or '--help'.");
        return 2;
    }

    private static string ReadRequiredValue(string[] args, ref int index, string optionName)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {optionName}.");
        }

        index++;
        return args[index];
    }

    private static int ParseInt(string raw, string optionName)
    {
        if (!int.TryParse(raw, out var value))
        {
            throw new ArgumentException($"Value for {optionName} must be an integer.");
        }

        return value;
    }

    private static int PromptInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Error.Write(prompt);
            var raw = Console.ReadLine();
            if (int.TryParse(raw, out var value) && value >= min && value <= max)
            {
                return value;
            }

            Console.Error.WriteLine($"Enter an integer between {min} and {max}.");
        }
    }

    private static string PromptRequired(string prompt)
    {
        while (true)
        {
            Console.Error.Write(prompt);
            var value = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            Console.Error.WriteLine("Value is required.");
        }
    }

    private static string PromptChoice(string prompt, string optionOne, string optionTwo, string defaultOption)
    {
        while (true)
        {
            Console.Error.Write(prompt);
            var value = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultOption;
            }

            var normalized = value.Trim().ToLowerInvariant();
            if (normalized == optionOne || normalized == optionTwo)
            {
                return normalized;
            }

            Console.Error.WriteLine($"Choose '{optionOne}' or '{optionTwo}'.");
        }
    }

    private static void WriteError(string message)
    {
        Console.Error.WriteLine($"Error: {message}");
    }

    private sealed record SplitOptions
    {
        public int? ShareCount { get; set; }
        public int? Threshold { get; set; }
        public string? SecretText { get; set; }
        public string? SecretBase64 { get; set; }
        public string? OutputPath { get; set; }
    }

    private sealed record CombineOptions
    {
        public List<string> ShareJson { get; } = [];
        public string? SharesFile { get; set; }
        public bool AsText { get; set; }
    }
}
