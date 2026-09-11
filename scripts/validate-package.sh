#!/usr/bin/env bash
set -euo pipefail

artifacts_dir="${1:-artifacts}"

if [[ ! -d "$artifacts_dir" ]]; then
  echo "Artifacts directory does not exist: $artifacts_dir" >&2
  exit 1
fi

mapfile -t packages < <(find "$artifacts_dir" -maxdepth 1 -type f -name 'MinimalOpenAPI.*.nupkg' -print | sort)
mapfile -t symbol_packages < <(find "$artifacts_dir" -maxdepth 1 -type f -name 'MinimalOpenAPI.*.snupkg' -print | sort)

if [[ ${#packages[@]} -ne 1 ]]; then
  echo "Expected exactly one MinimalOpenAPI .nupkg, found ${#packages[@]}." >&2
  printf '  %s\n' "${packages[@]:-<none>}" >&2
  exit 1
fi

if [[ ${#symbol_packages[@]} -ne 1 ]]; then
  echo "Expected exactly one MinimalOpenAPI .snupkg, found ${#symbol_packages[@]}." >&2
  printf '  %s\n' "${symbol_packages[@]:-<none>}" >&2
  exit 1
fi

package="${packages[0]}"
symbol_package="${symbol_packages[0]}"
nuspec="$(mktemp)"
trap 'rm -f "$nuspec"' EXIT

unzip -p "$package" '*.nuspec' > "$nuspec"

require_nuspec_text() {
  local text="$1"
  if ! grep -Fq "$text" "$nuspec"; then
    echo "Package nuspec does not contain expected text: $text" >&2
    exit 1
  fi
}

require_entry() {
  local entry="$1"
  if ! unzip -Z1 "$package" | grep -Fxq "$entry"; then
    echo "Package is missing expected entry: $entry" >&2
    exit 1
  fi
}

require_nuspec_text '<id>MinimalOpenAPI</id>'
require_nuspec_text '<authors>Renato Golia</authors>'
require_nuspec_text '<license type="expression">MIT</license>'
require_nuspec_text 'https://github.com/Kralizek/MinimalOpenApi'
require_nuspec_text '<description>Contract-first OpenAPI framework for ASP.NET Core.'

version="$(sed -n 's:.*<version>\([^<]*\)</version>.*:\1:p' "$nuspec" | head -n 1)"
if [[ -z "$version" ]]; then
  echo "Could not read the package version from the nuspec." >&2
  exit 1
fi

expected_filename="MinimalOpenAPI.${version}.nupkg"
if [[ "$(basename "$package")" != "$expected_filename" ]]; then
  echo "Package filename does not match nuspec version." >&2
  echo "Expected: $expected_filename" >&2
  echo "Actual:   $(basename "$package")" >&2
  exit 1
fi

required_entries=(
  'README.md'
  'lib/net10.0/MinimalOpenAPI.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.Abstractions.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.Parser.Yaml.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.Parser.Json.dll'
  'analyzers/dotnet/cs/YamlDotNet.dll'
  'build/MinimalOpenAPI.targets'
  'buildTransitive/MinimalOpenAPI.targets'
)

for entry in "${required_entries[@]}"; do
  require_entry "$entry"
done

if ! unzip -Z1 "$symbol_package" | grep -Eq '\.pdb$'; then
  echo "Symbol package does not contain any PDB files." >&2
  exit 1
fi

echo "Validated $(basename "$package") and $(basename "$symbol_package")."
