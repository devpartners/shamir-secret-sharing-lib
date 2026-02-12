# shamir-secret-sharing-lib

Shamir Secret Sharing in C# (`net10.0`) with:

- `ShamirSecretSharing.Core`: class library
- `ShamirSecretSharing.Core.Tests`: xUnit tests
- `ShamirSecretSharing.Console`: CLI for split/combine

## Build and test

```bash
dotnet test src/ShamirSecretSharing.Core.Tests/ShamirSecretSharing.Core.Tests.csproj
```

## CLI usage

### Split a UTF-8 secret into shares

```bash
dotnet run --project src/ShamirSecretSharing.Console/ShamirSecretSharing.Console.csproj -- \
  split --shares 5 --threshold 3 --secret-text "correct horse battery staple"
```

This prints one JSON share per line.

### Split a base64 secret and write shares to a file

```bash
dotnet run --project src/ShamirSecretSharing.Console/ShamirSecretSharing.Console.csproj -- \
  split --shares 5 --threshold 3 --secret-base64 "AQIDBAU=" --out shares.jsonl
```

### Combine shares (default output is base64)

```bash
dotnet run --project src/ShamirSecretSharing.Console/ShamirSecretSharing.Console.csproj -- \
  combine --shares-file shares.jsonl
```

### Combine shares and decode as UTF-8 text

```bash
dotnet run --project src/ShamirSecretSharing.Console/ShamirSecretSharing.Console.csproj -- \
  combine --shares-file shares.jsonl --as-text
```

If required arguments are omitted, the CLI switches to interactive prompts.
