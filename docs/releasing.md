# Releasing MinimalOpenAPI

MinimalOpenAPI uses a workflow-driven release process. Maintainers start a release from GitHub Actions; the workflow calculates the version, validates the package as a downstream consumer would see it, creates the tag and GitHub Release, and publishes the package.

## Versioning

MinimalOpenAPI follows Semantic Versioning and uses MinVer to derive package versions from Git tags.

Configuration lives in `Directory.Build.props`. Release tags use the `v` prefix. The release workflow uses `calculate-next-version@v0.4` with a minimum version of `1.0.0`, and the selected release version is supplied to the build through `MinVerVersionOverride`.

The workflow supports `major`, `minor`, and `patch` bumps and the `stable`, `alpha`, `beta`, and `rc` channels.

Release-note ranges follow the shared release policy: releases within the same prerelease channel are incremental, while the first release in a new channel and stable releases generate notes from the previous stable release.

## Published package

The repository publishes one package: `MinimalOpenAPI`.

It contains:

- `lib/net10.0/MinimalOpenAPI.dll` for ASP.NET Core runtime services;
- the Roslyn generator and bundled parser assemblies under `analyzers/dotnet/cs/`;
- `build/` and `buildTransitive/` targets;
- the NuGet README;
- a portable-PDB symbol package.

`MinimalOpenAPI.Abstractions`, `MinimalOpenAPI.Parser.Yaml`, and `MinimalOpenAPI.Parser.Json` are implementation projects. Their assemblies are bundled inside the main package and are not published independently.

## Prerequisites

- Push access to the repository and permission to run the release workflow from `master`.
- NuGet.org ownership of `MinimalOpenAPI`.
- A NuGet Trusted Publisher configured for `.github/workflows/publish.yml`.

NuGet.org publication uses GitHub Actions OIDC through `nuget/login`; no long-lived NuGet API key is required.

## Preparing a release

Before releasing:

1. Ensure the intended release commit is on `master` and CI is green.
2. Update `CHANGELOG.md`.
3. Move new analyzer diagnostics from `AnalyzerReleases.Unshipped.md` to `AnalyzerReleases.Shipped.md` under the target version when appropriate.
4. Update README/NuGet README examples, feature-support documentation, and known limitations when needed.
5. Close or move issues assigned to the release milestone.

Keep an empty `## Unreleased` section at the top of the changelog.

## Release validation

Every workflow-driven release, including a dry run, performs the important release gates before anything is published:

1. shared restore, format verification, build, and tests;
2. pack `MinimalOpenAPI` with the calculated release version;
3. run `scripts/validate-package.sh` against the produced package;
4. restore `sample/SmokeTest/SmokeTest.csproj` with `./artifacts` as a package source, ensuring it consumes the package produced by this run;
5. build and publish the smoke-test application;
6. verify that an authored OpenAPI schema is present below the publish output's `openapi/schemas/` directory.

The package-layout and downstream-consumption checks are MOA-specific release gates. They intentionally remain in this repository rather than in the shared GitHub Actions.

## Dry run

Use **Actions → Release → Run workflow** and enable `dry_run` to validate a release candidate without publishing it.

Choose the same version bump and release channel you intend to use for the real release.

A dry run calculates the intended version and release-note baseline, builds, tests, packs, validates the package, and runs the downstream-consumption smoke test.

A dry run does **not**:

- create or push a Git tag;
- create a GitHub Release;
- publish to GitHub Packages;
- publish to NuGet.org.

## Cutting a prerelease

1. Ensure `master` contains the exact code and release records you want to publish.
2. Open **Actions → Release → Run workflow**.
3. Select the version bump.
4. Select `alpha`, `beta`, or `rc`.
5. Run a dry run first when you want to validate the candidate without publishing.
6. Run the workflow with `dry_run` disabled to publish.

The workflow calculates the prerelease sequence, creates and pushes the immutable version tag, creates a GitHub prerelease with generated release notes, publishes the package, and attaches the package artifacts to the release.

Do not create the release tag or GitHub Release manually.

## Cutting a stable release

1. Confirm the intended release candidate has been validated.
2. Merge the final release-record and repository-polish changes.
3. Confirm required checks pass on `master`.
4. Open **Actions → Release → Run workflow**.
5. Select the required version bump and `stable` as the release channel.
6. Run a dry run if desired.
7. Run the workflow with `dry_run` disabled to publish.

The workflow creates the version tag and GitHub Release and publishes the exact package that passed the release validation.

Do not create the stable tag or GitHub Release manually.

## Publishing destinations

A successful non-dry-run release publishes the same validated package to:

- GitHub Packages;
- the GitHub Release as `.nupkg` and `.snupkg` assets;
- NuGet.org.

GitHub Packages is configured only when publishing. It is not required as a restore source for the repository build.

## Post-release checklist

- [ ] Confirm the GitHub Release and NuGet.org package have the expected version.
- [ ] Confirm the release notes use the intended channel baseline.
- [ ] Confirm the `.nupkg` and `.snupkg` assets are attached to the GitHub Release.
- [ ] Confirm symbols and Source Link behavior.
- [ ] Confirm the NuGet README renders correctly.
- [ ] Install the package in a clean .NET 10 ASP.NET Core project.
- [ ] Close the release milestone and completed issues.
- [ ] Keep `## Unreleased` at the top of the changelog.
