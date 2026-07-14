#!/usr/bin/env bash
# PreToolUse hook: block `gh pr create` unless CHANGELOG.md was updated on this branch.
#
# Claude Code passes the Bash tool's command in $TOOL_INPUT_COMMAND. If the command
# creates a pull request and CHANGELOG.md has not been touched (committed on the branch
# or pending in the working tree), the hook exits non-zero to stop the PR from being created.

set -euo pipefail

cmd="${TOOL_INPUT_COMMAND:-}"

# Only act on pull-request creation.
if ! printf '%s' "$cmd" | grep -qE 'gh[[:space:]]+pr[[:space:]]+create'; then
    exit 0
fi

repo_root="$(git rev-parse --show-toplevel 2>/dev/null || echo .)"
cd "$repo_root"

# Determine the base branch (default to 'main').
base="main"
git show-ref --verify --quiet "refs/heads/$base" || base="$(git symbolic-ref --quiet --short HEAD || echo main)"

changed=false

# Committed on this branch since it diverged from base.
if git diff --name-only "$base...HEAD" 2>/dev/null | grep -qx 'CHANGELOG.md'; then
    changed=true
fi

# Pending (staged or unstaged) changes to the changelog.
if git status --porcelain -- CHANGELOG.md 2>/dev/null | grep -q .; then
    changed=true
fi

if [ "$changed" = "true" ]; then
    exit 0
fi

echo 'BLOCK: CHANGELOG.md was not updated on this branch. Add an entry under a [YYYY-MM-DD] date heading before creating the PR.' >&2
exit 2
