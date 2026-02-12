@echo off
REM Script pour compiler le serveur de synchronisation PlocoManager
REM Ce script génère un exécutable standalone qui peut être lancé sans .NET SDK

echo ========================================
echo Compilation du Serveur de Synchronisation
echo ========================================
echo.

cd /d "%~dp0"

echo Nettoyage des anciens fichiers...
if exist "publish" rmdir /s /q "publish"

echo.
echo Compilation en cours...
dotnet publish PlocoSync.Server\PlocoSync.Server.csproj -c Release -r win-x64 --self-contained false -o publish\PlocoSync.Server

if %ERRORLEVEL% EQU 0 (
    echo.
    echo ========================================
    echo Compilation reussie !
    echo ========================================
    echo.
    echo Le serveur a ete compile dans : %~dp0publish\PlocoSync.Server
    echo.
    echo Pour lancer le serveur :
    echo   1. Ouvrir un terminal
    echo   2. Aller dans : %~dp0publish\PlocoSync.Server
    echo   3. Executer : PlocoSync.Server.exe
    echo.
    echo Le serveur sera accessible sur : http://localhost:5000
    echo.
) else (
    echo.
    echo ========================================
    echo ERREUR lors de la compilation !
    echo ========================================
    echo.
)

pause
