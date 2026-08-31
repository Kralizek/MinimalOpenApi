#!/usr/bin/env bash
set -euo pipefail

artifacts_dir="${1:-artifacts}"

mapfile -t packages < <(find "$artifacts_dir" -maxdepth 1 -type f -name 'MinimalOpenAPI.Client.*.nupkg' -print | sort)
mapfile -t symbol_packages < <(find "$artifacts_dir" -maxdepth 1 -type f -name 'MinimalOpenAPI.Client.*.snupkg' -print | sort)

if [[ ${#packages[@]} -ne 1 ]]; then
  echo "Expected exactly one MinimalOpenAPI.Client .nupkg, found ${#packages[@]}." >&2
  exit 1
fi

if [[ ${#symbol_packages[@]} -ne 1 ]]; then
  echo "Expected exactly one MinimalOpenAPI.Client .snupkg, found ${#symbol_packages[@]}." >&2
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
    echo "Client package nuspec does not contain expected text: $text" >&2
    exit 1
  fi
}

require_entry() {
  local entry="$1"
  if ! unzip -Z1 "$package" | grep -Fxq "$entry"; then
    echo "Client package is missing expected entry: $entry" >&2
    exit 1
  fi
}

require_nuspec_text '<id>MinimalOpenAPI.Client</id>'
require_nuspec_text '<authors>Renato Golia</authors>'
require_nuspec_text '<license type="expression">MIT</license>'
require_nuspec_text 'https://github.com/Kralizek/MinimalOpenApi'
require_nuspec_text 'Microsoft.Extensions.Http'

required_entries=(
  'README.md'
  'analyzers/dotnet/cs/MinimalOpenAPI.Client.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.Abstractions.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.Parser.Yaml.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.Parser.Json.dll'
  'analyzers/dotnet/cs/YamlDotNet.dll'
  'build/MinimalOpenAPI.Client.targets'
  'buildTransitive/MinimalOpenAPI.Client.targets'
)

for entry in "${required_entries[@]}"; do
  require_entry "$entry"
done

if ! unzip -Z1 "$symbol_package" | grep -Eq '\.pdb$'; then
  echo "Client symbol package does not contain any PDB files." >&2
  exit 1
fi

echo "Validated $(basename "$package") and $(basename "$symbol_package")."