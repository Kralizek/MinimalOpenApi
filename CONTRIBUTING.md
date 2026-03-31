# Contributing

Contributions are welcome — thanks for taking the time!

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (see `global.json` for the required version)
- A C# IDE or editor (Visual Studio, Rider, VS Code with C# Dev Kit)

## Building locally

```bash
dotnet build
```

The build enforces `--warnaserror`, so zero warnings are expected. Fix any warnings before opening a PR.

## Running the tests

```bash
dotnet test
```

This runs all unit and integration tests. Make sure everything passes before submitting.

## Running the sample app

```bash
cd sample/MinimalOpenAPI.Sample.Api
dotnet run
```

The sample exposes a simple Todo API. It is a good way to verify end-to-end behaviour after source-generator changes.

## Pull request expectations

- **Open an issue first** for anything non-trivial so we can agree on the approach before you invest time coding.
- Keep changes **small and focused** — one logical change per PR.
- Update or add tests for any changed behaviour.
- Update documentation (README, XML docs, `docs/`) if relevant.
- The CI pipeline must be green before a PR can be merged.

## Code style

The repository includes an `.editorconfig`. Your IDE should pick it up automatically.

## Reporting issues

Please use the issue templates in `.github/ISSUE_TEMPLATE/` when opening bugs or feature requests.
