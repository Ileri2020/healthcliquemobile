@echo off
setlocal EnableExtensions

set "PROJECT_DIR=%~dp0.."
for %%I in ("%PROJECT_DIR%") do set "PROJECT_DIR=%%~fI"
set "PROJECT=%PROJECT_DIR%\EEE.csproj"
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

if not defined FASTRELOAD_FRAMEWORK set "FASTRELOAD_FRAMEWORK=windows"
if /i "%FASTRELOAD_FRAMEWORK%"=="windows" (
	set "FRAMEWORK=net10.0-windows10.0.19041.0"
) else if /i "%FASTRELOAD_FRAMEWORK%"=="android" (
	set "FRAMEWORK=net10.0-android"
) else (
	echo ERROR: FASTRELOAD_FRAMEWORK must be windows or android.
	exit /b 1
)

if /i "%FASTRELOAD_FRAMEWORK%"=="android" call :check_android
if errorlevel 1 exit /b 1

call "%~dp0checkinstallrequiredpackages.bat" --check-only
if errorlevel 1 exit /b 1

pushd "%PROJECT_DIR%"
if /i "%~1"=="--help" (
	echo Usage: set FASTRELOAD_FRAMEWORK=windows^|android, then run %~nx0 [dotnet-watch-options]
	"%DOTNET%" watch --help
	set "RESULT=%ERRORLEVEL%"
	popd
	exit /b %RESULT%
)
echo Starting .NET hot reload for %FRAMEWORK%. Press Ctrl+C to stop.
"%DOTNET%" watch --project "%PROJECT%" run --framework "%FRAMEWORK%" %*
set "RESULT=%ERRORLEVEL%"
popd
exit /b %RESULT%

:check_android
if not defined ANDROID_SDK_ROOT if defined ANDROID_HOME set "ANDROID_SDK_ROOT=%ANDROID_HOME%"
if not defined ANDROID_SDK_ROOT set "ANDROID_SDK_ROOT=%LOCALAPPDATA%\Android\Sdk"
if not defined ANDROID_HOME set "ANDROID_HOME=%ANDROID_SDK_ROOT%"
if not exist "%ANDROID_SDK_ROOT%\platforms\android-36\android.jar" (
	echo ERROR: Android SDK Platform 36 was not found at "%ANDROID_SDK_ROOT%".
	exit /b 1
)
if not defined JAVA_HOME (
	echo ERROR: JAVA_HOME is not set. Install JDK 21 and set JAVA_HOME to its installation folder.
	exit /b 1
)
if not exist "%JAVA_HOME%\bin\java.exe" (
	echo ERROR: JAVA_HOME does not point to a JDK: "%JAVA_HOME%".
	exit /b 1
)
exit /b 0
