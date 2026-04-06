@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul 2>&1
title FotoEnvio - Compilador

echo ============================================================
echo   FotoEnvio - Gerando FotoEnvio.exe
echo ============================================================
echo.

:: Tenta encontrar o dotnet.exe em varios locais
set DOTNET=
for %%P in (dotnet.exe) do set DOTNET=%%~$PATH:P

if not defined DOTNET (
    if exist "%ProgramFiles%\dotnet\dotnet.exe"                      set DOTNET=%ProgramFiles%\dotnet\dotnet.exe
    if exist "%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe"            set DOTNET=%LOCALAPPDATA%\Microsoft\dotnet\dotnet.exe
    if exist "C:\Program Files\dotnet\dotnet.exe"                    set DOTNET=C:\Program Files\dotnet\dotnet.exe
    if exist "C:\Program Files (x86)\dotnet\dotnet.exe"              set DOTNET=C:\Program Files (x86)\dotnet\dotnet.exe
)

if not defined DOTNET (
    echo [ERRO] .NET SDK nao encontrado em nenhum local padrao.
    echo.
    echo Verifique se o .NET 8 SDK esta instalado:
    echo   Abra o Prompt de Comando e digite:  dotnet --version
    echo.
    echo Se nao estiver, baixe em:
    echo   https://dotnet.microsoft.com/download/dotnet/8.0
    echo   (Clique em "SDK 8.0.x" - Windows x64 Installer)
    echo.
    pause
    exit /b 1
)

echo [OK] .NET SDK encontrado em: %DOTNET%
"%DOTNET%" --version
echo.

echo [1/2] Restaurando pacotes NuGet...
"%DOTNET%" restore "%~dp0FotoEnvio.csproj" --nologo
if %ERRORLEVEL% NEQ 0 (
    echo [ERRO] Falha ao restaurar pacotes. Verifique internet.
    pause & exit /b 1
)

echo.
echo [2/2] Publicando executavel unico...
"%DOTNET%" publish "%~dp0FotoEnvio.csproj" ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  /p:PublishSingleFile=true ^
  /p:EnableCompressionInSingleFile=true ^
  /p:IncludeNativeLibrariesForSelfExtract=true ^
  -o "%~dp0dist" ^
  --nologo

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo [ERRO] Falha na compilacao.
    pause & exit /b 1
)

if exist "%~dp0dist\FotoEnvio.exe" (
    echo.
    echo ============================================================
    echo   SUCESSO! Executavel gerado em:
    echo   %~dp0dist\FotoEnvio.exe
    echo ============================================================
    explorer "%~dp0dist"
) else (
    echo [ERRO] FotoEnvio.exe nao foi gerado.
)
pause
