#!/usr/bin/env bash
# Run CodeHub on the standard port (8090).
# Builds the dashboard and backend, then starts the server.
set -e
cd "$(dirname "$0")/src"
echo ""
echo "Open your browser to http://127.0.0.1:8090/dashboard to access CodeHub"
echo ""
dotnet run --project CodeHub.Server -- --port 8090
