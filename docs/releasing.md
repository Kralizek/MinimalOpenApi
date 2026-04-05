# Releasing MinimalOpenAPI

This document explains how versioning works and how to cut beta, RC, and stable releases.

---

## Versioning

MinimalOpenAPI uses [MinVer](https://github.com/adamralph/minver) for automatic version calculation from Git tags and follows [Semantic Versioning](https://semver.org).

### How MinVer computes versions

MinVer derives the package version by inspecting the nearest Git tag that matches the tag prefix:

| Git state | Computed version |
|-----------|-----------------|
| Exactly on tag `v1.0.0` | `1.0.0` |
| Exactly on tag `v1.0.0-beta.1` | `1.0.0-beta.1` |
| 3 commits after `v1.0.0-beta.1` | `1.0.0-beta.1.3` (height appended) |
| No tags in history | `0.0.0-preview.0.{height}` |

Configuration in [`Directory.Build.props`](../Directory.Build.props):

```xml
<MinVerTagPrefix>v</MinVerTagPrefix>
<MinVerDefaultPreReleaseIdentifiers>preview</MinVerDefaultPreReleaseIdentifiers>
<MinVerAutoIncrement>minor</MinVerAutoIncrement>
```

- **Tag prefix:** `v` — tags must be `v1.0.0`, `v1.0.0-beta.1`, etc.
- **Default pre-release identifier:** `preview` — used when no tag exists yet; results in versions like `0.1.0-preview.0.42`.
- **Auto increment:** `minor` — after a stable tag, un-tagged commits produce a `minor`-bumped pre-release (e.g. after `v1.0.0`, the next un-tagged build produces `1.1.0-preview.0.1`).

### Version scheme summary

```
1.0.0-alpha     ← early development, no compatibility guarantees
1.0.0-beta.1    ← public pre-release, APIs may change
1.0.0-beta.2    ← subsequent betas
1.0.0-rc.1      ← release candidate, no planned breaking changes
1.0.0-rc.2      ← subsequent RCs if needed
1.0.0           ← stable release
```

---

## Prerequisites

- .NET 10 SDK (`dotnet --version` should show `10.x.x`)
- Push access to the repository
- Write access to the GitHub Releases page (for stable releases)
- A **NuGet Trusted Publisher** configured on nuget.org for each package (replaces the old `NUGET_API_KEY` secret — see [Trusted Publishing setup](#trusted-publishing-setup) below)

---

## Trusted Publishing setup

MinimalOpenAPI uses [NuGet Trusted Publishing](https://learn.microsoft.com/nuget/nuget-org/publish-a-package#trusted-publishing) to push packages to NuGet.org.
This mechanism uses short-lived OIDC tokens issued by GitHub Actions instead of a long-lived API key secret, eliminating the need to store `NUGET_API_KEY` in repository settings.

### One-time NuGet.org configuration

For **each package** published by this repository, a Trusted Publisher must be registered once on nuget.org:

1. Sign in to [nuget.org](https://www.nuget.org) with the package owner account.
2. Navigate to the package → **Manage package** → **Trusted Publishers**.
3. Click **Add trusted publisher** and select **GitHub Actions**.
4. Fill in:
   | Field | Value |
   |-------|-------|
   | Repository owner | `Kralizek` |
   | Repository name | `MinimalOpenApi` |
   | Workflow file name | `publish.yml` |
   | Environment | _(leave blank)_ |
5. Save. NuGet.org will now accept OIDC tokens issued by that specific workflow.

No repository secret is required after this one-time step.

### How it works in CI

The [`publish.yml`](../.github/workflows/publish.yml) workflow has `id-token: write` permission, which allows it to request a short-lived GitHub OIDC token.
The [`nuget/login`](https://github.com/NuGet/login) action exchanges that token with the NuGet.org token service and outputs a short-lived `NUGET_API_KEY` that is passed to `dotnet nuget push`.
NuGet.org validates the OIDC claims against the registered Trusted Publisher and accepts or rejects the push.

---

## Cutting a release

### 1. Update the changelog

Before tagging, update [`CHANGELOG.md`](../CHANGELOG.md):

1. Move all items from `## Unreleased` into a new versioned section.
2. Write a short, factual summary of what changed (added, changed, fixed, removed).
3. Commit the changelog update directly to `master` (or via a PR).

Example:

```markdown
## 1.0.0-beta.1

### Added
- Support for `nullable: true` on query parameters.

### Fixed
- MOA004 diagnostic now includes the file path in the message.
```

### 2. Validate package metadata

Before tagging, do a local pack to confirm metadata looks correct:

```shell
dotnet restore
dotnet build --configuration Release --warnaserror
dotnet pack src/MinimalOpenAPI/MinimalOpenAPI.csproj --no-build --configuration Release --output ./artifacts
```

Inspect the generated `.nupkg` file:

```shell
unzip -p ./artifacts/MinimalOpenAPI.*.nupkg '*.nuspec' | less
```

Check that:
- `<version>` matches the intended release version
- `<authors>`, `<licenseExpression>`, `<repositoryUrl>` are correct
- `<description>` is accurate
- `<developmentDependency>true</developmentDependency>` is present
- `lib/net10.0/MinimalOpenAPI.dll` and all expected analyzer DLLs are present

### 3. Create and push the version tag

Tags drive the version. Create a tag matching the `v` prefix:

```shell
# For a beta release
git tag v1.0.0-beta.1
git push origin v1.0.0-beta.1

# For a release candidate
git tag v1.0.0-rc.1
git push origin v1.0.0-rc.1

# For a stable release
git tag v1.0.0
git push origin v1.0.0
```

Pushing the tag triggers the [Publish workflow](#publishing-workflow) automatically only for stable releases (GitHub Release published event). For beta and RC releases, you need to publish manually (see below).

---

## Publishing workflow

The [`publish.yml`](../.github/workflows/publish.yml) workflow handles all publishing.
Only the `MinimalOpenAPI` package is published; the `MinimalOpenAPI.Abstractions`,
`MinimalOpenAPI.Parser.Yaml`, and `MinimalOpenAPI.Parser.Json` projects have
`<IsPackable>false</IsPackable>` and are not published — their DLLs are bundled
inside the main package.

| Trigger | What happens |
|---------|-------------|
| **Manual dispatch** (`workflow_dispatch`) | Builds, packs, and pushes packages to GitHub Packages (nightly / pre-release feed). |
| **GitHub Release published** (`release: published`) | Builds, packs, uploads `.nupkg`/`.snupkg` to the GitHub Release assets, and pushes to NuGet.org. |

### Cutting a beta or RC release (manual)

1. Push the pre-release tag (e.g. `v1.0.0-beta.1`) as shown above.
2. Verify the build passes on the tag commit in the [Actions tab](https://github.com/Kralizek/MinimalOpenApi/actions).
3. Go to **Actions → Publish → Run workflow** to push the packages to GitHub Packages.
4. Optionally: create a pre-release GitHub Release (`v1.0.0-beta.1`, marked as pre-release) to publish to NuGet.org automatically.

### Cutting a stable release

1. Ensure the changelog is up to date and all tests pass on `master`.
2. Push the stable tag:
   ```shell
   git tag v1.0.0
   git push origin v1.0.0
   ```
3. On GitHub, go to **Releases → Draft a new release**.
4. Select the tag `v1.0.0`.
5. Copy the relevant section from `CHANGELOG.md` into the release notes.
6. Click **Publish release**.
7. The Publish workflow runs automatically and pushes to NuGet.org.
8. Verify the packages appear on [nuget.org](https://www.nuget.org/packages/MinimalOpenAPI) within a few minutes.

---

## Validating packages locally

To test a local pack before pushing any tag:

```shell
# Build and pack in Release configuration
dotnet pack src/MinimalOpenAPI/MinimalOpenAPI.csproj --configuration Release --output /tmp/local-packages

# Add a local NuGet source (one-time)
dotnet nuget add source /tmp/local-packages --name local-MinimalOpenAPI

# Reference the package in a test project
dotnet add <TestProject> package MinimalOpenAPI --source local-MinimalOpenAPI --prerelease

# Remove the local source when done
dotnet nuget remove source local-MinimalOpenAPI
```

To inspect the contents of a `.nupkg` file directly (it is a ZIP archive):

```shell
unzip -l /tmp/local-packages/MinimalOpenAPI.1.0.0-beta.1.nupkg
```

---

## Post-release checklist

After a stable release:

- [ ] Confirm packages are live on NuGet.org.
- [ ] Confirm `.snupkg` symbol packages are indexed (NuGet.org symbol server).
- [ ] Update `CHANGELOG.md` — add a new `## Unreleased` heading at the top.
- [ ] Bump any `Version="..."` references in sample or documentation if they pinned the previous version.
- [ ] Close any GitHub issues or milestones associated with this release.
