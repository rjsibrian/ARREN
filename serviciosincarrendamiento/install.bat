@echo off
TITLE Instalador Servicio - Sincronizacion Arrendamiento POS
SETLOCAL
cls

:: Configuración
SET "SERVICE_NAME=ServicioSincArrendamiento"
SET "SERVICE_DISPLAY_NAME=Servicio de Sincronizacion de Arrendamiento POS"
SET "EXECUTABLE_NAME=Servicio.exe"
SET "FULL_EXECUTABLE_PATH=%~dp0publish\%EXECUTABLE_NAME%"
SET "LOG_FILE=%~dp0install.log"

echo.
echo  ================================================================
echo    INSTALADOR DE SERVICIO WINDOWS
echo  ================================================================
echo.

:: Iniciar log
echo [%DATE% %TIME%] Iniciando instalacion >> "%LOG_FILE%"

:: Verificar permisos de administrador
echo Verificando permisos...
net session >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo ERROR: Se requieren permisos de Administrador.
    echo Ejecute como administrador y vuelva a intentar.
    echo [%DATE% %TIME%] ERROR: Sin permisos de administrador >> "%LOG_FILE%"
    pause
    EXIT /B 1
)
echo OK - Permisos verificados
echo [%DATE% %TIME%] Permisos OK >> "%LOG_FILE%"

:: Verificar ejecutable
echo Verificando archivos...
IF NOT EXIST "%FULL_EXECUTABLE_PATH%" (
    echo ERROR: No se encontro el ejecutable en: %FULL_EXECUTABLE_PATH%
    echo [%DATE% %TIME%] ERROR: Ejecutable no encontrado >> "%LOG_FILE%"
    pause
    EXIT /B 1
)
echo OK - Ejecutable encontrado
echo [%DATE% %TIME%] Ejecutable encontrado >> "%LOG_FILE%"

:: Detener y eliminar servicio existente
echo Limpiando instalacion anterior...
sc query "%SERVICE_NAME%" >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    echo - Deteniendo servicio...
    sc stop "%SERVICE_NAME%" >nul 2>&1
    timeout /t 2 /nobreak >nul
    echo - Eliminando servicio...
    sc delete "%SERVICE_NAME%" >nul 2>&1
    timeout /t 1 /nobreak >nul
    echo [%DATE% %TIME%] Servicio anterior eliminado >> "%LOG_FILE%"
)

:: Crear nuevo servicio
echo Instalando servicio...
sc create "%SERVICE_NAME%" binPath= "\"%FULL_EXECUTABLE_PATH%\"" displayName= "%SERVICE_DISPLAY_NAME%" start= auto >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo ERROR: No se pudo crear el servicio
    echo [%DATE% %TIME%] ERROR: Fallo en creacion del servicio >> "%LOG_FILE%"
    pause
    EXIT /B 1
)
sc description "%SERVICE_NAME%" "Servicio para sincronizar arrendamiento POS" >nul 2>&1
echo OK - Servicio creado
echo [%DATE% %TIME%] Servicio creado >> "%LOG_FILE%"

:: Iniciar servicio
echo Iniciando servicio...
sc start "%SERVICE_NAME%" >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo ADVERTENCIA: El servicio se instalo pero no se pudo iniciar
    echo Puede iniciarlo manualmente desde services.msc
    echo [%DATE% %TIME%] ADVERTENCIA: Servicio no iniciado >> "%LOG_FILE%"
    pause
    EXIT /B 0
)

:: Verificar estado
timeout /t 3 /nobreak >nul
sc query "%SERVICE_NAME%" | find "RUNNING" >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    echo.
    echo  ================================================================
    echo   INSTALACION EXITOSA
    echo  ================================================================
    echo   Servicio: %SERVICE_DISPLAY_NAME%
    echo   Estado: EJECUTANDOSE
    echo   Inicio: AUTOMATICO
    echo.
    echo [%DATE% %TIME%] Instalacion exitosa - Servicio ejecutandose >> "%LOG_FILE%"
) ELSE (
    echo ADVERTENCIA: Servicio instalado pero estado incierto
    echo Verifique en services.msc
    echo [%DATE% %TIME%] ADVERTENCIA: Estado del servicio incierto >> "%LOG_FILE%"
)

echo Log guardado en: %LOG_FILE%
echo.
echo Presione cualquier tecla para salir...
pause >nul
ENDLOCAL
EXIT /B 0