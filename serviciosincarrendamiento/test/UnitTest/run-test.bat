@echo off
REM test.bat - Script de ejecución de tests con cobertura para UnitTest
setlocal

echo 🧪 Ejecutando tests con cobertura en UnitTest...

REM Determinar la ruta del script
set SCRIPT_PATH=%~dp0

REM Limpiar directorios anteriores
if exist "%SCRIPT_PATH%coveragereport" rmdir /s /q "%SCRIPT_PATH%coveragereport"
if exist "%SCRIPT_PATH%TestResults" rmdir /s /q "%SCRIPT_PATH%TestResults"

REM Crear directorios
mkdir "%SCRIPT_PATH%coveragereport"

REM Ejecutar tests con cobertura
echo 🧪 Ejecutando tests...
pushd "%SCRIPT_PATH%"
dotnet test UnitTest.csproj --collect:"XPlat Code Coverage" --settings:coverlet.runsettings --results-directory:"TestResults" --output:"bin/TestOutput"
popd

if %errorlevel% neq 0 (
    echo ❌ Tests fallaron o no alcanzaron el umbral de cobertura
    pause
    exit /b 1
)

REM Verificar e instalar ReportGenerator si es necesario
reportgenerator --help >nul 2>&1
if %errorlevel% neq 0 (
    echo 📦 Instalando ReportGenerator...
    dotnet tool install --global dotnet-reportgenerator-globaltool
)

REM Generar reporte HTML
echo 📊 Generando reporte HTML...
reportgenerator -reports:"%SCRIPT_PATH%TestResults\*\coverage.cobertura.xml" -targetdir:"%SCRIPT_PATH%coveragereport" -reporttypes:"Html;HtmlSummary;Badges"

if %errorlevel% neq 0 (
    echo ❌ Error generando reporte HTML
    pause
    exit /b 1
)

REM Abrir reporte en navegador
echo 🌐 Abriendo reporte...
start "" "%SCRIPT_PATH%coveragereport\index.html"

echo ✅ ¡Proceso completado!
echo 📁 Reporte disponible en: %SCRIPT_PATH%coveragereport\index.html
echo 📊 Tests ejecutados: ReportingServiceTests, SyncServiceTests, WorkerTests
pause 