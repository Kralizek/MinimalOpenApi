#!/usr/bin/env bash
# Package-consumption smoke test for the class-library layout (sample/ClassLibrarySmokeTest).
#
# Restores, builds (--warnaserror) and publishes the host against the MinimalOpenAPI package in
# artifacts/, then runs the published host and checks the endpoints of every OpenAPI document:
# two declared in referenced class libraries and one declared by the host itself.
#
# Usage: scripts/smoke-test-class-library.sh [publish-dir] [port]
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
host_project="$repo_root/sample/ClassLibrarySmokeTest/Host/Host.csproj"
publish_dir="${1:-${RUNNER_TEMP:-${TMPDIR:-/tmp}}/class-library-smoke-publish}"
port="${2:-5187}"
base_url="http://127.0.0.1:$port"

echo "::group::Restore, build and publish"
dotnet restore "$host_project" --force --no-cache
dotnet build "$host_project" --no-restore --configuration Release --warnaserror
rm -rf "$publish_dir"
dotnet publish "$host_project" --no-restore --configuration Release --output "$publish_dir"
echo "::endgroup::"

log_file="$publish_dir/host.log"
dotnet "$publish_dir/Host.dll" --urls "$base_url" > "$log_file" 2>&1 &
host_pid=$!
trap 'kill "$host_pid" 2>/dev/null || true' EXIT

for _ in $(seq 1 60); do
  if curl --silent --output /dev/null "$base_url/health"; then
    break
  fi
  if ! kill -0 "$host_pid" 2>/dev/null; then
    echo "Published host exited during startup:" >&2
    cat "$log_file" >&2
    exit 1
  fi
  sleep 1
done

failures=0

# expect <path> <expected status> <expected body fragment> [expected header]
expect() {
  local path="$1" status="$2" fragment="$3" header="${4:-}"
  local headers body actual
  headers="$(mktemp)"
  body="$(curl --silent --show-error --dump-header "$headers" "$base_url$path" || true)"
  actual="$(head -n 1 "$headers" | awk '{print $2}')"

  if [[ "$actual" != "$status" ]]; then
    echo "FAIL $path: expected status $status, got '${actual:-<none>}'" >&2
    failures=$((failures + 1))
  elif [[ -n "$fragment" && "$body" != *"$fragment"* ]]; then
    echo "FAIL $path: body does not contain '$fragment': $body" >&2
    failures=$((failures + 1))
  elif [[ -n "$header" ]] && ! grep -qi "^$header" "$headers"; then
    echo "FAIL $path: missing response header '$header'" >&2
    failures=$((failures + 1))
  else
    echo "ok   $path -> $status"
  fi
  rm -f "$headers"
}

expect /catalog/items/42 200 '"name":"Item 42"'
expect /catalog/items/0 404 ''
expect /billing/invoices/2 200 '"total":21' 'X-Billing-Configured: true'
expect /health 200 '"status":"ok"'

if [[ $failures -ne 0 ]]; then
  echo "$failures check(s) failed. Host log:" >&2
  cat "$log_file" >&2
  exit 1
fi

if grep -q "mapped no endpoints" "$log_file"; then
  echo "FAIL: the host logged that no MinimalOpenAPI endpoints were mapped." >&2
  exit 1
fi

echo "Class-library smoke test passed."
