# AGENTS.md — Coding-agent guide for MinimalOpenAPI

This file is the operational reference for automated coding agents working in this repository.
It is not a replacement for [`README.md`](README.md) or [`CONTRIBUTING.md`](CONTRIBUTING.md) — read those first for background.

---

**Audience separation:** This file is for agents modifying the MinimalOpenAPI repository. For agents using MinimalOpenAPI in another application, use [`skills/minimalopenapi/SKILL.md`](skills/minimalopenapi/SKILL.md) or [`docs/consumer-agents.md`](docs/consumer-agents.md). Keep these resources in sync when consumer APIs change.

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
  SmokeTest/           ← CI/package-consumption sample; validates the packed NuGet artifact
  BasicTodo/           ← recommended learning starting point; minimal contract-first Todo API
  SchemaPublishing/    ← demonstrates PublishAs, MapOpenApiSchemas(), and Swagger UI wiring
  Parameters/          ← demonstrates all parameter kinds (path, query, header, cookie, component $ref)
  SchemaShapes/        ← demonstrates DTO shapes (enums, allOf, readOnly/writeOnly, additionalProperties)
  ResponseResults/     ← demonstrates typed result and problem wrapper types
  GeneratedFiles/      ← demonstrates EmitCompilerGeneratedFiles to inspect generated output
  MultipartNested/     ← multipart form fields and nested objects
  ResponseResults/     ← typed response handling and middleware-owned statuses
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
- **Public API changes need extra care** — the project is post-1.0; follow SemVer and justify breaking changes.

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
cd sample/BasicTodo
dotnet run
```

For source-generator changes, also run the smoke test to confirm the packed artifact works as a real NuGet consumer:

```bash
# 1. Pack into the artifacts/ directory that the smoke-test reads from
dotnet pack --configuration Release --output artifacts

# 2. Restore and build the smoke-test consumer (exercises the generator end-to-end)
dotnet restore sample/SmokeTest/SmokeTest.csproj
dotnet build sample/SmokeTest/SmokeTest.csproj --no-restore --configuration Release --warnaserror

# 3. Publish the smoke-test consumer (exercises the OpenAPI file publish targets)
dotnet publish sample/SmokeTest/SmokeTest.csproj --no-restore --configuration Release --output /tmp/smoketest-publish
# Verify the spec file was published at the expected path:
# Verify the spec file was published under the unique schema-id subdirectory:
find /tmp/smoketest-publish/openapi/schemas -name "openapi.yaml" | grep -q . && echo "OpenAPI spec published OK"
```

---

## 6. Task-specific guidance

### Source generator changes
- Cover the change with a new or updated generator test in `MinimalOpenAPI.Generator.Tests`.
- Also run `MinimalOpenAPI.IntegrationTests` to catch end-to-end regressions.
- Run the smoke test (pack + build `sample/SmokeTest`) to confirm the packed NuGet artifact still works as a real downstream consumer would experience it.
- Diagnostics live in `MinimalOpenAPI.Generator`; the codes are `MOA001`–`MOA017` (see `docs/architecture.md` §8).

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
- Consider backward compatibility — since `1.0.0` has shipped, follow SemVer for breaking changes. Describe consumer-visible changes under `Unreleased` without rewriting published release sections.

### Changelog policy

The changelog describes the **consumer-visible public surface** of the package. Published release sections must be preserved, while unreleased content may be curated.
The project is post-1.0.0: curate the `## Unreleased` section for upcoming changes, and preserve published release sections as historical records.

**Before touching `CHANGELOG.md`, ask two questions in order:**

1. **Is the change visible to package consumers?** If not, skip the changelog entirely.
2. **Is the change already represented by an existing entry?** If so, update or rewrite that entry instead of adding a new one.

**Do not add changelog items for:**
- documentation-only changes
- CI/CD or workflow changes
- test-only changes
- refactorings with no consumer-visible effect
- internal implementation cleanup
- repository structure changes with no consumer impact
- dependency updates that do not affect consumers

**Add a new changelog item only when** the change introduces a genuinely new consumer-visible capability or behavior that is not already represented by an existing entry.

---

## 7. Completion checklist

Before closing the task, confirm all of the following:

- [ ] `dotnet format --verify-no-changes` passes
- [ ] `dotnet build --warnaserror` passes with zero warnings
- [ ] `dotnet test` — all tests pass
- [ ] New behaviour is covered by tests (generator tests, runtime tests, or integration tests as appropriate)
- [ ] `README.md`, `docs/`, and XML doc comments are updated if public behaviour changed
- [ ] `CHANGELOG.md` updated only if the change is consumer-visible and not already represented by an existing entry (see changelog policy in §6)
- [ ] `dotnet pack` output looks correct for packaging/release changes
- [ ] Sample app runs and the relevant endpoints work for generator/runtime changes
- [ ] No unrelated files are modified
