#!/bin/zsh
set -euo pipefail

REPO_DIR="$(cd "$(dirname "$0")" && pwd)"
DOTNET_BIN="/usr/local/share/dotnet/x64/dotnet"

if [[ ! -x "$DOTNET_BIN" ]]; then
  echo "dotnet was not found at $DOTNET_BIN"
  echo "If you installed it somewhere else, update this script."
  exit 1
fi

export DOTNET_CLI_HOME="${DOTNET_CLI_HOME:-/tmp}"
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

cd "$REPO_DIR"
"$DOTNET_BIN" run --no-launch-profile --project Apps/KnightsAndGM.Api/KnightsAndGM.Api.csproj --urls http://127.0.0.1:5055
