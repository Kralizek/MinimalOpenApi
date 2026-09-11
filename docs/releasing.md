# Releasing MinimalOpenAPI

This document describes versioning, package validation, prerelease publication, and stable releases.

## Versioning

MinimalOpenAPI follows [Semantic Versioning](https://semver.org/) and uses [MinVer](https://github.com/adamralph/minver) to derive package versions from Git tags.

Configuration lives in [`Directory.Build.props`](../Directory.Build.props):

```xml
<MinVerTagPrefix>v</MinVerTagPrefix>
<MinVerDefaultPreReleaseIdentifiers>preview</MinVerDefaultPreReleaseIdentifiers>
<MinVerAutoIncrement>minor</MinVerAutoIncrement>
```

Examples:

| Git state | Package version |
|---|---|
| Exactly on `v1.0.0` | `1.0.0` |
| Exactly on `v1.0.0-rc.3` | `1.0.0-rc.3` |
| Commits after a prerelease tag | The prerelease version plus MinVer height metadata |
| Commits after `v1.0.0` | A `1.1.0-preview...` development version |

Tags must use the `v` prefix.

## Published package

The repository publishes one package: `MinimalOpenAPI`.

It contains:

- `lib/net10.0/MinimalOpenAPI.dll` for ASP.NET Core runtime services;
- the Roslyn generator and bundled parser assemblies under `analyzers/dotnet/cs/`;
- `build/` and `buildTransitive/` targets;
- the NuGet README;
- a portable-PDB symbol package.

`MinimalOpenAPI.Abstractions`, `MinimalOpenAPI.Parser.Yaml`, and `MinimalOpenAPI.Parser.Json` are implementation projects with `<IsPackable>false</IsPackable>`. Their assemblies are bundled inside the main package and are not published independently.

## Prerequisites

- .NET 10 SDK matching [`global.json`](../global.json)
- Push access to the repository
- Permission to create tags and GitHub Releases
- NuGet.org ownership of the `MinimalOpenAPI` package
- A NuGet Trusted Publisher configured for `.github/workflows/publish.yml`

## NuGet Trusted Publishing

NuGet.org publication uses GitHub Actions OIDC rather than a long-lived API key.

Configure the package once on NuGet.org:

1. Open **Manage package → Trusted Publishers**.
2. Add a **GitHub Actions** trusted publisher.
3. Use:

   | Field | Value |
   |---|---|
   | Repository owner | `Kralizek` |
   | Repository name | `MinimalOpenApi` |
   | Workflow file | `publish.yml` |
   | Environment | Leave blank unless the workflow is later moved to a protected environment |

The publish workflow requires `id-token: write` and uses `nuget/login` to obtain a short-lived token.

## Preparing a release

### 1. Freeze behavior

For a stable release, avoid unrelated feature work after the final release candidate. Only release blockers and repository/release corrections should land between the final RC and the stable tag.

### 2. Update release records

Before tagging:

- update [`CHANGELOG.md`](../CHANGELOG.md);
- move new analyzer diagnostics from `AnalyzerReleases.Unshipped.md` to `AnalyzerReleases.Shipped.md` under the target version;
- update README and NuGet README examples when the documented stable version changes;
- update the feature support matrix and known limitations;
- close or move issues assigned to the release milestone.

Keep an empty `## Unreleased` section at the top of the changelog for subsequent work.

### 3. Validate locally

Run the same checks expected by CI and publication:

```shell
dotnet restore
dotnet format --verify-no-changes --no-restore
dotnet build --no-restore --configuration Release --warnaserror
dotnet test --no-build --configuration Release

dotnet pack src/MinimalOpenAPI/MinimalOpenAPI.csproj \
  --no-build \
  --configuration Release \
  --output ./artifacts

bash scripts/validate-package.sh ./artifacts

dotnet restore sample/SmokeTest/SmokeTest.csproj --force --no-cache
dotnet build sample/SmokeTest/SmokeTest.csproj \
  --no-restore \
  --configuration Release \
  --warnaserror
dotnet publish sample/SmokeTest/SmokeTest.csproj \
  --no-restore \
  --configuration Release \
  --output /tmp/minimalopenapi-smoke
```

Confirm that the smoke-test publish output contains an authored OpenAPI document below `openapi/schemas/`.

### 4. Inspect package metadata

The validation script checks the package layout, README, repository metadata, MIT license expression, and symbol package.

For manual inspection:

```shell
unzip -p ./artifacts/MinimalOpenAPI.*.nupkg '*.nuspec' | less
unzip -l ./artifacts/MinimalOpenAPI.*.nupkg
unzip -l ./artifacts/MinimalOpenAPI.*.snupkg
```

Confirm that:

- the package and nuspec versions match the intended tag;
- authors, description, license, repository URL, and project URL are correct;
- runtime, analyzer, parser, targets, and README files are present;
- no unintended public NuGet dependencies are introduced;
- the symbol package contains portable PDBs.

## Publishing workflows

[`.github/workflows/publish.yml`](../.github/workflows/publish.yml) supports two paths:

| Trigger | Destination |
|---|---|
| Manual `workflow_dispatch` | GitHub Packages |
| Published GitHub Release | GitHub Release assets and NuGet.org |

Both paths restore, build, test, pack, validate package contents, and consume the produced package through the clean smoke-test project before publishing.

A published GitHub Release can be either a prerelease or a stable release. The workflow validates that:

- the release tag is valid Semantic Versioning with a `v` prefix;
- the checked-out commit is exactly the release tag;
- tags with a prerelease suffix are marked as GitHub prereleases;
- stable tags are not marked as prereleases.

## Cutting a prerelease

1. Ensure the target commit is green.
2. Create and push a tag such as:

   ```shell
   git tag v1.1.0-rc.1
   git push origin v1.1.0-rc.1
   ```

3. Create a GitHub Release for that tag and mark it as a **prerelease**.
4. Publish the release.
5. Verify the workflow and NuGet.org package.
6. Consume the published package from a clean external project.

Use manual workflow dispatch only when a GitHub Packages build is desired without publishing a GitHub Release or NuGet.org package.

## Cutting a stable release

1. Confirm the final release candidate has been validated externally.
2. Merge the final release-record and repository-polish changes.
3. Confirm all required checks pass on `master`.
4. Tag the exact validated commit:

   ```shell
   git tag v1.0.0
   git push origin v1.0.0
   ```

5. Draft a GitHub Release from `v1.0.0`.
6. Use the corresponding changelog section as the release notes, editing it for readability where useful.
7. Publish the release.
8. Verify:
   - `.nupkg` and `.snupkg` assets are attached to the GitHub Release;
   - the package is available on NuGet.org;
   - symbols are indexed;
   - the NuGet README renders correctly;
   - Source Link resolves repository source;
   - installation succeeds in a new .NET 10 ASP.NET Core project.

## Post-release checklist

- [ ] Confirm the GitHub Release and NuGet.org package show the same version and notes.
- [ ] Confirm symbol-server and Source Link behavior.
- [ ] Close the release milestone and completed issues.
- [ ] Keep `## Unreleased` at the top of the changelog.
- [ ] Ensure development builds now resolve to the next MinVer preview line.
- [ ] Announce any known limitations that are especially relevant to existing prerelease users.
