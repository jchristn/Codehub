@echo off
REM Run CodeHub on the standard port (8090).
REM Builds the dashboard and backend, then starts the server.
REM Optional first argument selects the target framework, e.g.:  go.bat net8.0
REM Defaults to net10.0.
setlocal
set "FRAMEWORK=%~1"
if "%FRAMEWORK%"=="" set "FRAMEWORK=net10.0"
pushd "%~dp0src"
echo.
echo Open your browser to http://127.0.0.1:8090/dashboard to access CodeHub
echo.
dotnet run --project CodeHub.Server --framework %FRAMEWORK% -- --port 8090
popd
endlocal
