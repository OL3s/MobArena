#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cat >&2 <<'EOF'
Resetting local save data and creating a funded demo run.
This intentionally deletes user://save for this Godot project.
EOF

cd "$ROOT_DIR"
exec tools/cli.sh --delete --generate-company --add-money=250 --generate-gladiator "$@"
