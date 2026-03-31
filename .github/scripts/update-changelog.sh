#!/usr/bin/env bash
# .github/scripts/update-changelog.sh
#
# Ensures CHANGELOG.md has a versioned section for the given version.
#  - If the section already exists, prints a message and exits 0.
#  - If not, inserts a new "## <version>" section after "## Unreleased",
#    leaving the Unreleased heading in place for future work.
#
# Usage: update-changelog.sh <version>
# Environment: CHANGELOG_PATH overrides the default CHANGELOG.md path.

set -euo pipefail

VERSION="${1:?Usage: $0 <version>}"
CHANGELOG="${CHANGELOG_PATH:-CHANGELOG.md}"

if [ ! -f "$CHANGELOG" ]; then
  echo "ERROR: $CHANGELOG not found" >&2
  exit 1
fi

# If the section already exists, nothing to do.
if grep -qF "## ${VERSION}" "$CHANGELOG"; then
  echo "Section '## ${VERSION}' already exists in ${CHANGELOG} — nothing to do."
  exit 0
fi

# Require ## Unreleased to be present before modifying.
if ! grep -qE '^## Unreleased$' "$CHANGELOG"; then
  echo "ERROR: No '## Unreleased' section found in ${CHANGELOG}." >&2
  echo "Please add a '## Unreleased' heading before running this script." >&2
  exit 1
fi

# Insert the new versioned section after ## Unreleased.
# Python is always available on GitHub-hosted runners.
python3 - "$CHANGELOG" "$VERSION" << 'PYEOF'
import sys
import re

path, version = sys.argv[1], sys.argv[2]

with open(path) as f:
    text = f.read()

m = re.search(r'^## Unreleased[ \t]*$', text, re.MULTILINE)
if not m:
    print("ERROR: ## Unreleased not found", file=sys.stderr)
    sys.exit(1)

# Find the start of the next ## heading after ## Unreleased.
after = text[m.end():]
next_h = re.search(r'^##[ \t]', after, re.MULTILINE)
insert_at = m.end() + (next_h.start() if next_h else len(after))

new_section = f'\n## {version}\n\n_Placeholder -- release notes will be added on publish._\n\n'
result = text[:insert_at] + new_section + text[insert_at:]

with open(path, 'w') as f:
    f.write(result)

print(f"Added '## {version}' section to {path}.")
PYEOF
