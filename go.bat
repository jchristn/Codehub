@echo off
REM Run CodeHub on the standard port (8090).
REM Builds the dashboard and backend, then starts the server.
setlocal
pushd "%~dp0src"
echo.
echo Open your browser to http://127.0.0.1:8090/dashboard to access CodeHub
echo.
dotnet run --project CodeHub.Server -- --port 8090
popd
endlocal
