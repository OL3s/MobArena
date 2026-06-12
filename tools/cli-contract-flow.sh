#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

cd "$ROOT_DIR"
exec tools/cli.sh --generate-company-if-missing --add-money=250 --generate-gladiator --complete-contract --next-day "$@"
