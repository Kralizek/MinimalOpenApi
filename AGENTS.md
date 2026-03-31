# AGENTS.md — Coding-agent guide for MinimalOpenAPI

This file is the operational reference for automated coding agents working in this repository.
It is not a replacement for [`README.md`](README.md) or [`CONTRIBUTING.md`](CONTRIBUTING.md) — read those first for background.

---

## 1. Repository purpose

MinimalOpenAPI is a **contract-first** OpenAPI framework for ASP.NET Core Minimal APIs.
You author an OpenAPI YAML or JSON spec; a Roslyn incremental source generator reads it at build time and emits DTO records, abstract handler base classes, DI registration, and endpoint mapping.
The developer only implements the business logic.

See [`docs/architecture.md`](docs/architecture.md) for a full design walk-through.

---

## 2. Repository layout

```
src/
  MinimalOpenAPI/               ← NuGet entry point; contains the Roslyn source generator
  MinimalOpenAPI.Runtime/       ← runtime services (AddMinimalOpenApi / MapMinimalOpenApiEndpoints)
  MinimalOpenAPI.Abstractions/  ← OpenAPI document model + IOpenApiParser interface
  MinimalOpenAPI.Parser.Yaml/   ← YAML parser (IOpenApiParser implementation)
  MinimalOpenAPI.Parser.Json/   ← JSON parser (IOpenApiParser implementation)
tests/
  MinimalOpenAPI.Generator.Tests/   ← Roslyn driver tests; feed fixtures to the generator
  MinimalOpenAPI.Runtime.Tests/     ← unit tests for runtime services
  MinimalOpenAPI.IntegrationTests/  ← WebApplicationFactory end-to-end tests
sample/
  MinimalOpenAPI.Sample.Api/    ← full Todo CRUD example; use it to verify end-to-end behaviour
  MinimalOpenAPI.SmokeTest.Api/ ← minimal consumer that builds against the packed NuGet artifact; validates the generator works as a real downstream project would experience it
docs/
  architecture.md               ← internals, data flow, design decisions, extensibility
  releasing.md                  ← versioning (MinVer / Git tags) and release process
.github/
  workflows/                    ← CI (ci.yml) and publish (publish.yml) pipelines
  ISSUE_TEMPLATE/               ← issue templates
```

---

## 3. Before making changes

Depending on the task, inspect the relevant files before touching any code:

| Task type | Files to read first |
|-----------|---------------------|
| Generator change | `docs/architecture.md` §2–§9, `src/MinimalOpenAPI/`, `tests/MinimalOpenAPI.Generator.Tests/` |
| Runtime change | `docs/architecture.md` §4.2, `src/MinimalOpenAPI.Runtime/`, `tests/MinimalOpenAPI.Runtime.Tests/` |
| Parser change | `docs/architecture.md` §4.3–§4.5 and §10, `src/MinimalOpenAPI.Parser.Yaml/` or `.Json/` |
| Type-mapping change | `docs/architecture.md` §7, `src/MinimalOpenAPI/TypeMapper.cs` |
| Packaging / release | `docs/releasing.md`, `Directory.Build.props`, `Directory.Packages.props` |
| New feature | `README.md` (limitations), `docs/architecture.md` |

---

## 4. Working rules

- **Keep PRs small and focused** — one logical change per PR.
- **No unrelated refactors** — fix only what the task requires.
- **Preserve the package split** unless the task explicitly requires restructuring.
- **Extend existing patterns** (e.g. new type-mapping entry in `TypeMapper`, new parser following the `IOpenApiParser` pattern) rather than inventing new ones.
- **Update docs and tests when behaviour changes** — if generated output changes, update generator test fixtures; if runtime behaviour changes, update runtime/integration tests; if public APIs change, update `README.md` and XML docs.
- **Public API changes need extra care** — the project is moving toward 1.0; breaking changes require justification.

---

## 5. Required validation before finishing

Run all three checks from the repository root before declaring work done:

```bash
# 1. Format validation
dotnet format --verify-no-changes

# 2. Build — warnings are treated as errors
dotnet build --warnaserror

# 3. All tests
dotnet test
```

For packaging or release changes, also validate locally:

```bash
dotnet pack --configuration Release --output /tmp/local-packages
unzip -l /tmp/local-packages/MinimalOpenAPI.*.nupkg
```

For source-generator or runtime behaviour changes, also run the sample app and exercise the relevant endpoints:

```bash
cd sample/MinimalOpenAPI.Sample.Api
dotnet run
```

For source-generator changes, also run the smoke test to confirm the packed artifact works as a real NuGet consumer:

```bash
# 1. Pack into the artifacts/ directory that the smoke-test reads from
dotnet pack --configuration Release --output artifacts

# 2. Restore and build the smoke-test consumer (exercises the generator end-to-end)
dotnet restore sample/MinimalOpenAPI.SmokeTest.Api/SmokeTest.Api.csproj
dotnet build sample/MinimalOpenAPI.SmokeTest.Api/SmokeTest.Api.csproj --no-restore --configuration Release --warnaserror
```

---

## 6. Task-specific guidance

### Source generator changes
- Cover the change with a new or updated generator test in `MinimalOpenAPI.Generator.Tests`.
- Also run `MinimalOpenAPI.IntegrationTests` to catch end-to-end regressions.
- Run the smoke test (pack + build `MinimalOpenAPI.SmokeTest.Api`) to confirm the packed NuGet artifact still works as a real downstream consumer would experience it.
- Diagnostics live in `MinimalOpenAPI.Generator`; the codes are `MOA001`–`MOA005` (see `docs/architecture.md` §8).

### Runtime changes
- Cover the change with a new or updated test in `MinimalOpenAPI.Runtime.Tests`.
- If the startup hook (`[ModuleInitializer]`) or `MapMinimalOpenApiEndpoints` behaviour changes, run integration tests too.

### Parser changes
- Each parser (`Parser.Yaml`, `Parser.Json`) is an independent `IOpenApiParser` implementation.
- Adding a new format: follow `docs/architecture.md` §10 exactly — new project, implement `IOpenApiParser`, add the DLL as an `<Analyzer>` in `MinimalOpenAPI.targets`, extend `SelectParser`.

### Packaging / release changes
- Follow `docs/releasing.md` for tag naming, changelog updates, and the publish workflow.
- Validate package metadata locally with `dotnet pack` before pushing.

### Public API changes
- Document them in `README.md` (usage examples, limitations).
- Add XML doc comments to new public members.
- Consider backward compatibility — until `1.0.0` is tagged, breaking changes are permitted but must be flagged in `CHANGELOG.md`.

---

## 7. Completion checklist

Before closing the task, confirm all of the following:

- [ ] `dotnet format --verify-no-changes` passes
- [ ] `dotnet build --warnaserror` passes with zero warnings
- [ ] `dotnet test` — all tests pass
- [ ] New behaviour is covered by tests (generator tests, runtime tests, or integration tests as appropriate)
- [ ] `README.md`, `docs/`, and XML doc comments are updated if public behaviour changed
- [ ] `CHANGELOG.md` `## Unreleased` section reflects the change
- [ ] `dotnet pack` output looks correct for packaging/release changes
- [ ] Sample app runs and the relevant endpoints work for generator/runtime changes
- [ ] No unrelated files are modified
