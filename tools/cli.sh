#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

if [[ "$#" -eq 0 ]]; then
  cat >&2 <<'EOF'
Usage: tools/cli.sh <runtime-cli-flags>

Examples:
  tools/cli.sh --help
  tools/cli.sh --print-load
  tools/cli.sh --generate-company --add-money=250 --generate-gladiator

This wrapper passes flags after Godot's runtime separator:
  tools/godot-dotnet8.sh --headless -- <runtime-cli-flags>
EOF
  exit 2
fi

cd "$ROOT_DIR"
exec tools/godot-dotnet8.sh --headless -- "$@"
