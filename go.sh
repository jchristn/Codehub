#!/usr/bin/env bash
# Run CodeHub on the standard port (8090).
# Builds the dashboard and backend, then starts the server.
set -e
cd "$(dirname "$0")/src"
dotnet run --project CodeHub.Server -- --port 8090
