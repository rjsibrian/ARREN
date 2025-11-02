@echo off
TITLE Desinstalador de Servicio - Sincronizacion Arrendamiento POS
SETLOCAL
cls

echo.
echo  ================================================================
echo    DETENER Y DESINSTALAR SERVICIO
echo  ================================================================
echo.

:: --- 1. VERIFICACION DE PERMISOS DE ADMINISTRADOR ---
echo Verificando permisos de administrador...
net session >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo.
    echo  ERROR: Se requieren permisos de Administrador.
    echo.
    echo  Por favor, cierre esta ventana, haga clic derecho en
    echo  uninstall.bat y seleccione "Ejecutar como administrador".
    echo.
    pause
    EXIT /B 1
)
echo  -> Permisos OK.
echo.

:: --- 2. CONFIGURACION DEL SERVICIO ---
SET "SERVICE_NAME=ServicioSincArrendamiento"

:: --- 3. VERIFICACION DE EXISTENCIA DEL SERVICIO ---
echo Verificando si el servicio existe...
sc query "%SERVICE_NAME%" >nul 2>&1
IF %ERRORLEVEL% NEQ 0 (
    echo.
    echo  INFORMACION: El servicio "%SERVICE_NAME%" no esta instalado.
    echo  No hay nada que desinstalar.
    echo.
    pause
    EXIT /B 0
)
echo  -> Servicio encontrado.
echo.

:: --- 4. DETENCION DEL SERVICIO ---
echo Verificando estado del servicio...
sc query "%SERVICE_NAME%" | find "STATE" | find "RUNNING" >nul
IF %ERRORLEVEL% EQU 0 (
    echo  -> El servicio esta ejecutandose. Deteniendolo...
    sc stop "%SERVICE_NAME%" >nul
    IF %ERRORLEVEL% NEQ 0 (
        echo.
        echo  ADVERTENCIA: No se pudo detener el servicio automaticamente.
        echo  Intentando desinstalacion de todas formas...
        echo.
    ) ELSE (
        echo  -> Comando de detencion enviado. Esperando confirmacion...
        timeout /t 5 /nobreak >nul
        
        sc query "%SERVICE_NAME%" | find "STATE" | find "STOPPED" >nul
        IF %ERRORLEVEL% EQU 0 (
            echo  -> Servicio detenido correctamente.
        ) ELSE (
            echo  -> ADVERTENCIA: No se pudo confirmar la detencion del servicio.
        )
    )
) ELSE (
    echo  -> El servicio ya esta detenido.
)
echo.

:: --- 5. ELIMINACION DEL SERVICIO ---
echo Eliminando el servicio...
sc delete "%SERVICE_NAME%" >nul
IF %ERRORLEVEL% NEQ 0 (
    echo.
    echo  ERROR: No se pudo eliminar el servicio.
    echo  Posibles causas:
    echo  - El servicio aun esta en ejecucion
    echo  - Procesos dependientes activos
    echo  - Permisos insuficientes
    echo.
    echo  Intente reiniciar el sistema y ejecutar este script nuevamente.
    echo.
    pause
    EXIT /B 1
)
echo  -> Servicio eliminado del sistema.
echo.

:: --- 6. VERIFICACION FINAL ---
echo Verificando eliminacion...
timeout /t 2 /nobreak >nul
sc query "%SERVICE_NAME%" >nul 2>&1
IF %ERRORLEVEL% EQU 0 (
    echo.
    echo  ADVERTENCIA: El servicio aun aparece en el sistema.
    echo  Es posible que requiera reiniciar el sistema para completar la eliminacion.
    echo.
) ELSE (
    echo.
    echo  ================================================================
    echo   EXITO: El servicio fue eliminado correctamente del sistema.
    echo  ================================================================
    echo.
)

:: --- FIN ---
echo Presione cualquier tecla para cerrar esta ventana.
pause
ENDLOCAL
EXIT /B 0