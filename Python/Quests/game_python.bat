@echo off

set COMPANY_NAME=MattCastellanosPedroLeon
set PRODUCT_NAME=AICrossing

@REM Ruta de persistent data path de Unity
set PERSISTENT_DATA_PATH=%USERPROFILE%\AppData\LocalLow\%COMPANY_NAME%\%PRODUCT_NAME%
set DATA_DIRECTORY=%PERSISTENT_DATA_PATH%\Data\NPC

set PYTHON_FILE=%~dp0%main.py
@REM Se ejecuta el script de python que genera las quests
python "%PYTHON_FILE%"

@REM Deberia existir, pero se crea por precaucion
set OUTPUT_DIRECTORY=%~dp0%output
if not exist "%OUTPUT_DIRECTORY%" (
    echo Creating output directory: %OUTPUT_DIRECTORY%.
    mkdir "%OUTPUT_DIRECTORY%"
)

echo:
echo ========================================================
@REM Se crea el directorio si no existe
if not exist "%DATA_DIRECTORY%" (
    echo Creating Persistent Data Path directory: %DATA_DIRECTORY%
    mkdir "%DATA_DIRECTORY%"
)

echo Starting to copy quests to Persistent Data Path...
@REM Se copia el contenido de la carpeta output a persistent data path
xcopy /s /y "%OUTPUT_DIRECTORY%" "%DATA_DIRECTORY%"
echo Quests copied to Persistent Data Path successfully!
