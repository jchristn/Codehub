@echo off
REM Run CodeHub on the standard port (8090).
REM Builds the dashboard and backend, then starts the server.
setlocal
pushd "%~dp0src"
dotnet run --project CodeHub.Server -- --port 8090
popd
endlocal
