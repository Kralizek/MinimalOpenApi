#!/usr/bin/env bash
set -euo pipefail

artifacts_dir="${1:-artifacts}"

mapfile -t packages < <(find "$artifacts_dir" -maxdepth 1 -type f -name 'MinimalOpenAPIClient.*.nupkg' -print | sort)
mapfile -t symbol_packages < <(find "$artifacts_dir" -maxdepth 1 -type f -name 'MinimalOpenAPIClient.*.snupkg' -print | sort)

if [[ ${#packages[@]} -ne 1 ]]; then
  echo "Expected exactly one MinimalOpenAPIClient .nupkg, found ${#packages[@]}." >&2
  exit 1
fi

if [[ ${#symbol_packages[@]} -ne 0 ]]; then
  echo "Expected no MinimalOpenAPIClient .snupkg for the analyzer-only package." >&2
  exit 1
fi

package="${packages[0]}"
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

require_nuspec_text '<id>MinimalOpenAPIClient</id>'
require_nuspec_text '<authors>Renato Golia</authors>'
require_nuspec_text '<license type="expression">MIT</license>'
require_nuspec_text 'https://github.com/Kralizek/MinimalOpenApi'
require_nuspec_text 'Microsoft.Extensions.Http'

required_entries=(
  'README.md'
  'analyzers/dotnet/cs/MinimalOpenAPIClient.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.Abstractions.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.Parser.Yaml.dll'
  'analyzers/dotnet/cs/MinimalOpenAPI.Parser.Json.dll'
  'analyzers/dotnet/cs/YamlDotNet.dll'
  'build/MinimalOpenAPIClient.targets'
  'buildTransitive/MinimalOpenAPIClient.targets'
)

for entry in "${required_entries[@]}"; do
  require_entry "$entry"
done

echo "Validated $(basename "$package")."