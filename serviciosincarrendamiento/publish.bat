@echo off
setlocal

REM =================================================================================
REM Script de publicacion y despliegue para ServicioSincArrendamiento
REM =================================================================================

REM --- Configuracion de rutas ---
set "sourceProjectPath=src\Servicio\Servicio.csproj"
set "publishOutputPath=C:\publish\ServicioSincArrendamiento"
set "stagingPath=C:\publish\ServicioSincArrendamiento_staging"
set "zipFilePath=C:\publish\ServicioSincArrendamiento.zip"
REM ATENCION: Ajuste la ruta de destino segun sea necesario.
set "destinationPath=\\192.168.150.7\Files\Servicios\SincronizarArrendamiento"

echo.
echo --- Iniciando proceso de publicacion y despliegue ---
echo.
echo Proyecto: %sourceProjectPath%
echo Carpeta de publicacion: %publishOutputPath%
echo Carpeta de preparacion: %stagingPath%
echo Archivo ZIP: %zipFilePath%
echo Destino de copia: %destinationPath%
echo.

REM --- 1. Preparar carpetas ---
echo [1/6] Preparando carpetas...
if exist "%publishOutputPath%" (
    echo      Eliminando carpeta de publicacion existente...
    rmdir /S /Q "%publishOutputPath%"
)
if exist "%stagingPath%" (
    echo      Eliminando carpeta de preparacion existente...
    rmdir /S /Q "%stagingPath%"
)
echo      Creando carpetas limpias...
mkdir "%publishOutputPath%"
mkdir "%stagingPath%"
echo      Carpetas preparadas.
echo.

REM --- 2. Publicar el proyecto .NET ---
echo [2/6] Publicando el proyecto .NET (Release)...
dotnet publish "%sourceProjectPath%" -c Release -o "%publishOutputPath%" --no-cache
if %errorlevel% neq 0 (
    echo.
    echo ERROR: La publicacion del proyecto ha fallado.
    goto :error
)
echo      Publicacion completada.
echo.

REM --- 3. Preparar archivos para compresion ---
echo [3/6] Preparando estructura para el archivo ZIP...
echo      Creando subcarpeta 'publish'...
mkdir "%stagingPath%\publish"
echo      Moviendo archivos publicados a 'publish'...
move "%publishOutputPath%\*" "%stagingPath%\publish\"
echo      Copiando install.bat...
copy "install.bat" "%stagingPath%\"
if %errorlevel% neq 0 (
    echo.
    echo ERROR: No se pudo copiar install.bat. Asegurese de que el archivo existe.
    goto :error
)
echo      Estructura preparada.
echo.


REM --- 4. Eliminar archivo ZIP anterior y comprimir ---
echo [4/6] Comprimiendo la carpeta de preparacion...
if exist "%zipFilePath%" (
    echo      Eliminando archivo ZIP anterior...
    del /F /Q "%zipFilePath%"
)
powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "Add-Type -AssemblyName System.IO.Compression.FileSystem; [System.IO.Compression.ZipFile]::CreateFromDirectory('%stagingPath%', '%zipFilePath%')"
if %errorlevel% neq 0 (
    echo.
    echo ERROR: La compresion ha fallado.
    goto :error
)
echo      Compresion completada: %zipFilePath%
echo.

REM --- 5. Copiar archivos a destino ---
echo [5/6] Copiando archivos a la red...
echo      Destino: %destinationPath%
echo      Esto puede tardar unos minutos. Por favor, espere...
echo      Copiando %zipFilePath%...
copy /Y "%zipFilePath%" "%destinationPath%"
if %errorlevel% neq 0 (
    echo.
    echo ERROR: La copia del archivo ZIP ha fallado.
    goto :error_copy
)
echo      Copiando install.bat...
copy /Y "install.bat" "%destinationPath%"
if %errorlevel% neq 0 (
    echo.
    echo ERROR: La copia de install.bat ha fallado.
    goto :error_copy
)
echo      Archivos copiados exitosamente.
echo.

REM --- 6. Limpieza ---
echo [6/6] Limpiando carpetas temporales...
rmdir /S /Q "%publishOutputPath%"
REM La siguiente linea esta comentada para no eliminar la carpeta de 'staging' y permitir su inspeccion.
REM rmdir /S /Q "%stagingPath%"
echo      Limpieza completada.
echo.

REM --- Opcional: Limpieza del ZIP local ---
REM Descomente la siguiente linea para eliminar el archivo zip local despues de copiarlo.
REM del /F /Q "%zipFilePath%"

goto :success

:error_copy
echo      Verifique la ruta de destino y la conectividad de red.
goto :error

:error
echo.
echo --- PROCESO FALLIDO ---
pause
exit /b 1

:success
echo.
echo --- PROCESO COMPLETADO EXITOSAMENTE ---
pause
endlocal
exit /b 0
