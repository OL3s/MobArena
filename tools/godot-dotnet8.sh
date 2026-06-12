#!/usr/bin/env bash
set -euo pipefail

DOTNET_SOURCE_ROOT="${DOTNET_SOURCE_ROOT:-$HOME/.dotnet}"
DOTNET_ISOLATED_ROOT="${DOTNET_ISOLATED_ROOT:-/tmp/opencode/dotnet8-root}"
DOTNET_SDK_VERSION="${DOTNET_SDK_VERSION:-8.0.422}"
DOTNET_RUNTIME_VERSION="${DOTNET_RUNTIME_VERSION:-8.0.28}"

print_setup_help() {
  cat >&2 <<EOF

Godot .NET 8 wrapper setup help
--------------------------------
This wrapper isolates Godot Mono to .NET 8 because Godot can ignore global.json
during editor/import startup on systems with multiple .NET runtimes.

Expected local .NET root:
  DOTNET_SOURCE_ROOT=$DOTNET_SOURCE_ROOT

Expected versions:
  SDK:     $DOTNET_SDK_VERSION
  Runtime: $DOTNET_RUNTIME_VERSION

Install or configure one of these options:
  1. Install .NET 8 SDK into ~/.dotnet, then rerun this command.
     Linux quick install:
       curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
       bash /tmp/dotnet-install.sh --channel 8.0 --install-dir "$HOME/.dotnet"

  2. If .NET 8 is installed elsewhere, point the wrapper at it:
       DOTNET_SOURCE_ROOT=/path/to/dotnet-root tools/godot-dotnet8.sh --headless --import --quit

  3. If your installed .NET 8 patch differs, override the versions:
       DOTNET_SDK_VERSION=8.0.xxx DOTNET_RUNTIME_VERSION=8.0.yy tools/godot-dotnet8.sh --headless --import --quit

Useful checks:
  dotnet --list-sdks
  dotnet --list-runtimes
  which godot

EOF
}

if ! command -v godot >/dev/null 2>&1; then
  printf 'Missing required executable: godot\n' >&2
  printf 'Install Godot 4.6 .NET/Mono and ensure the godot command is on PATH.\n' >&2
  print_setup_help
  exit 1
fi

required_paths=(
  "$DOTNET_SOURCE_ROOT/host/fxr/$DOTNET_RUNTIME_VERSION"
  "$DOTNET_SOURCE_ROOT/shared/Microsoft.NETCore.App/$DOTNET_RUNTIME_VERSION"
  "$DOTNET_SOURCE_ROOT/shared/Microsoft.AspNetCore.App/$DOTNET_RUNTIME_VERSION"
  "$DOTNET_SOURCE_ROOT/sdk/$DOTNET_SDK_VERSION"
)

for required_path in "${required_paths[@]}"; do
  if [[ ! -e "$required_path" ]]; then
    printf 'Missing required .NET 8 path: %s\n' "$required_path" >&2
    print_setup_help
    exit 1
  fi
done

rm -rf "$DOTNET_ISOLATED_ROOT"
mkdir -p \
  "$DOTNET_ISOLATED_ROOT/host/fxr" \
  "$DOTNET_ISOLATED_ROOT/shared/Microsoft.NETCore.App" \
  "$DOTNET_ISOLATED_ROOT/shared/Microsoft.AspNetCore.App" \
  "$DOTNET_ISOLATED_ROOT/sdk"

ln -s "$DOTNET_SOURCE_ROOT/host/fxr/$DOTNET_RUNTIME_VERSION" "$DOTNET_ISOLATED_ROOT/host/fxr/$DOTNET_RUNTIME_VERSION"
ln -s "$DOTNET_SOURCE_ROOT/shared/Microsoft.NETCore.App/$DOTNET_RUNTIME_VERSION" "$DOTNET_ISOLATED_ROOT/shared/Microsoft.NETCore.App/$DOTNET_RUNTIME_VERSION"
ln -s "$DOTNET_SOURCE_ROOT/shared/Microsoft.AspNetCore.App/$DOTNET_RUNTIME_VERSION" "$DOTNET_ISOLATED_ROOT/shared/Microsoft.AspNetCore.App/$DOTNET_RUNTIME_VERSION"
ln -s "$DOTNET_SOURCE_ROOT/sdk/$DOTNET_SDK_VERSION" "$DOTNET_ISOLATED_ROOT/sdk/$DOTNET_SDK_VERSION"

export DOTNET_ROOT="$DOTNET_ISOLATED_ROOT"
export DOTNET_MULTILEVEL_LOOKUP=0

exec godot "$@"
