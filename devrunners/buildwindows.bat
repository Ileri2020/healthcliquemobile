@echo off
setlocal EnableExtensions

set "PROJECT_DIR=%~dp0.."
for %%I in ("%PROJECT_DIR%") do set "PROJECT_DIR=%%~fI"
set "PROJECT=%PROJECT_DIR%\EEE.csproj"
set "FRAMEWORK=net10.0-windows10.0.19041.0"
set "OUTPUT_DIR=%TEMP%\HealthClique-build\windows"
set "INSTALLER_DIR=%PROJECT_DIR%\Platforms\installer"
set "DOTNET=%ProgramFiles%\dotnet\dotnet.exe"
if not exist "%DOTNET%" set "DOTNET=dotnet"

if not exist "%PROJECT%" (
	echo ERROR: Could not find "%PROJECT%".
	exit /b 1
)

"%DOTNET%" --version >nul 2>nul
if errorlevel 1 (
	echo ERROR: .NET SDK not found. Install the .NET 10 SDK and restart the terminal.
	exit /b 1
)

call "%~dp0checkinstallrequiredpackages.bat" --check-only
if errorlevel 1 exit /b 1

if not exist "%INSTALLER_DIR%" mkdir "%INSTALLER_DIR%"
if errorlevel 1 exit /b 1

pushd "%PROJECT_DIR%"
"%DOTNET%" publish "%PROJECT%" -f "%FRAMEWORK%" -c Release --artifacts-path "%OUTPUT_DIR%" -p:PublishReadyToRun=false -p:WindowsPackageType=MSIX -p:GenerateAppxPackageOnBuild=true -p:AppxPackageSigningEnabled=false -p:AppxBundle=Never -p:AppxPackageDir="%INSTALLER_DIR%\\" %*
set "RESULT=%ERRORLEVEL%"
popd
if not "%RESULT%"=="0" exit /b %RESULT%

set "PACKAGE="
for /f "delims=" %%F in ('dir /s /b "%INSTALLER_DIR%\*.msix" 2^>nul') do set "PACKAGE=%%F"
if not defined PACKAGE (
	echo ERROR: The publish succeeded but no MSIX was found in "%INSTALLER_DIR%".
	exit /b 1
)

echo Windows installer created:
echo %PACKAGE%
echo Note: This MSIX is unsigned. Sign it with a trusted certificate before installation.
exit /b 0
