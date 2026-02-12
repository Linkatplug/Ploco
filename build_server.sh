#!/bin/bash
# Script pour compiler le serveur de synchronisation PlocoManager
# Ce script génère un exécutable standalone qui peut être lancé sans .NET SDK

echo "========================================"
echo "Compilation du Serveur de Synchronisation"
echo "========================================"
echo ""

cd "$(dirname "$0")"

echo "Nettoyage des anciens fichiers..."
rm -rf publish

echo ""
echo "Compilation en cours..."
dotnet publish PlocoSync.Server/PlocoSync.Server.csproj -c Release -r linux-x64 --self-contained false -o publish/PlocoSync.Server

if [ $? -eq 0 ]; then
    echo ""
    echo "========================================"
    echo "Compilation réussie !"
    echo "========================================"
    echo ""
    echo "Le serveur a été compilé dans : $(pwd)/publish/PlocoSync.Server"
    echo ""
    echo "Pour lancer le serveur :"
    echo "  1. Ouvrir un terminal"
    echo "  2. Aller dans : $(pwd)/publish/PlocoSync.Server"
    echo "  3. Exécuter : ./PlocoSync.Server"
    echo ""
    echo "Le serveur sera accessible sur : http://localhost:5000"
    echo ""
else
    echo ""
    echo "========================================"
    echo "ERREUR lors de la compilation !"
    echo "========================================"
    echo ""
fi
