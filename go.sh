#!/usr/bin/env bash
# Run CodeHub on the standard port (8090).
# Builds the dashboard and backend, then starts the server.
# Optional first argument selects the target framework, e.g.:  ./go.sh net8.0
# Defaults to net10.0.
set -e
FRAMEWORK="${1:-net10.0}"
cd "$(dirname "$0")/src"
echo ""
echo "Open your browser to http://127.0.0.1:8090/dashboard to access CodeHub"
echo ""
dotnet run --project CodeHub.Server --framework "$FRAMEWORK" -- --port 8090
