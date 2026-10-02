@echo off
setlocal
rem Builds the release: dist\mdl-demo.exe (one file, runs without .NET
rem installed) and dist\mdl-demo-unsigned.zip. Needs the .NET 10 SDK:
rem   winget install Microsoft.DotNet.SDK.10

set "ROOT=%~dp0.."
set "DIST=%ROOT%\dist"

rem A terminal opened before the SDK was installed does not have it on PATH yet.
set "DOTNET=dotnet"
where dotnet >nul 2>&1
if errorlevel 1 set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
if not exist "%DOTNET%" if /i not "%DOTNET%"=="dotnet" (
    echo build-release: the .NET SDK is not installed - run: winget install Microsoft.DotNet.SDK.10 1>&2
    exit /b 1
)

if exist "%DIST%" rmdir /s /q "%DIST%"
rem The whole publish directory is zipped, so a leftover file would ship too.
if exist "%~dp0MDL_Demo\bin\publish" rmdir /s /q "%~dp0MDL_Demo\bin\publish"

set "DOTNET_NOLOGO=1"
"%DOTNET%" publish "%~dp0MDL_Demo" -p:PublishProfile=win-x64
if errorlevel 1 exit /b 1

echo.
dir /b "%DIST%"
