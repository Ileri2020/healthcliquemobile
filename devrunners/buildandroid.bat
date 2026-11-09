@echo off
setlocal EnableExtensions

set "PROJECT_DIR=%~dp0.."
for %%I in ("%PROJECT_DIR%") do set "PROJECT_DIR=%%~fI"
set "PROJECT=%PROJECT_DIR%\EEE.csproj"
set "FRAMEWORK=net10.0-android"
set "OUTPUT_DIR=%PROJECT_DIR%\Platforms\dist"
set "ARTIFACTS_DIR=%TEMP%\HealthClique-build\android"
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

if not defined ANDROID_SDK_ROOT if defined ANDROID_HOME set "ANDROID_SDK_ROOT=%ANDROID_HOME%"
if not defined ANDROID_SDK_ROOT set "ANDROID_SDK_ROOT=%LOCALAPPDATA%\Android\Sdk"
if not defined ANDROID_HOME set "ANDROID_HOME=%ANDROID_SDK_ROOT%"
if not exist "%ANDROID_SDK_ROOT%\platforms\android-36\android.jar" (
	echo ERROR: Android SDK Platform 36 was not found at "%ANDROID_SDK_ROOT%".
	echo Install Android SDK Platform 36 and Build Tools 36.0.0 with Android Studio, then retry.
	exit /b 1
)
if not exist "%ANDROID_SDK_ROOT%\build-tools\36.0.0" (
	echo ERROR: Android Build Tools 36.0.0 were not found in "%ANDROID_SDK_ROOT%".
	echo Install Android SDK Build Tools 36.0.0 with Android Studio, then retry.
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

if not defined ANDROID_RUNTIME_IDENTIFIER set "ANDROID_RUNTIME_IDENTIFIER=android-arm64"
if /i not "%ANDROID_RUNTIME_IDENTIFIER%"=="android-arm64" if /i not "%ANDROID_RUNTIME_IDENTIFIER%"=="android-x64" if /i not "%ANDROID_RUNTIME_IDENTIFIER%"=="android-x86" (
	echo ERROR: ANDROID_RUNTIME_IDENTIFIER must be android-arm64, android-x64, or android-x86.
	exit /b 1
)

call "%JAVA_HOME%\bin\java.exe" -version
if errorlevel 1 exit /b 1

pushd "%PROJECT_DIR%"
"%DOTNET%" publish "%PROJECT%" -f "%FRAMEWORK%" -c Release --artifacts-path "%ARTIFACTS_DIR%" -p:PublishReadyToRun=false -p:PublishTrimmed=false -p:RunAOTCompilation=false -p:AndroidPackageFormats=apk -p:RuntimeIdentifier=%ANDROID_RUNTIME_IDENTIFIER% %*
set "RESULT=%ERRORLEVEL%"
popd
if not "%RESULT%"=="0" exit /b %RESULT%

set "PACKAGE="
for /f "delims=" %%F in ('dir /s /b "%ARTIFACTS_DIR%\*-Signed.apk" 2^>nul') do set "PACKAGE=%%F"
if not defined PACKAGE (
	echo ERROR: The publish succeeded but no signed APK was found under "%ARTIFACTS_DIR%".
	exit /b 1
)

set "APK_NAME=HealthClique-%ANDROID_RUNTIME_IDENTIFIER%.apk"
copy /y "%PACKAGE%" "%OUTPUT_DIR%\%APK_NAME%" >nul
if errorlevel 1 exit /b 1
echo Android APK created:
echo %OUTPUT_DIR%\%APK_NAME%
exit /b 0
