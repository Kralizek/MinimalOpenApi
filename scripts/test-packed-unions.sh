#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
shopt -s nullglob
packages=(artifacts/MinimalOpenAPI.*.nupkg)
if (( ${#packages[@]} != 1 )); then
  echo "Expected exactly one packed MinimalOpenAPI package in artifacts/." >&2
  exit 1
fi

version="${packages[0]#artifacts/MinimalOpenAPI.}"
version="${version%.nupkg}"
project=tests/MinimalOpenAPI.UnionCompatibilityTests/MinimalOpenAPI.UnionCompatibilityTests.csproj
dotnet restore "$project" --configfile sample/SmokeTest/nuget.config "-p:PackedMinimalOpenApiVersion=$version"
dotnet test "$project" --no-restore --configuration Release "-p:PackedMinimalOpenApiVersion=$version" --warnaserror
