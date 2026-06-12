#!/usr/bin/env bash
set -euo pipefail

DOTNET_SOURCE_ROOT="${DOTNET_SOURCE_ROOT:-$HOME/.dotnet}"
DOTNET_ISOLATED_ROOT="${DOTNET_ISOLATED_ROOT:-/tmp/opencode/dotnet8-root}"
DOTNET_SDK_VERSION="${DOTNET_SDK_VERSION:-8.0.422}"
DOTNET_RUNTIME_VERSION="${DOTNET_RUNTIME_VERSION:-8.0.28}"

required_paths=(
  "$DOTNET_SOURCE_ROOT/host/fxr/$DOTNET_RUNTIME_VERSION"
  "$DOTNET_SOURCE_ROOT/shared/Microsoft.NETCore.App/$DOTNET_RUNTIME_VERSION"
  "$DOTNET_SOURCE_ROOT/shared/Microsoft.AspNetCore.App/$DOTNET_RUNTIME_VERSION"
  "$DOTNET_SOURCE_ROOT/sdk/$DOTNET_SDK_VERSION"
)

for required_path in "${required_paths[@]}"; do
  if [[ ! -e "$required_path" ]]; then
    printf 'Missing required .NET 8 path: %s\n' "$required_path" >&2
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
