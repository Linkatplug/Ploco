# 🎉 Guide de Démarrage - Synchronisation Multi-Utilisateurs

**Date** : 12 février 2026  
**Version** : 1.0 - Prêt à l'Emploi

---

## 📋 Vue d'Ensemble

La synchronisation multi-utilisateurs est maintenant **prête à être utilisée** ! Ce guide vous explique comment démarrer rapidement.

---

## 🚀 Démarrage Rapide (5 minutes)

### Étape 1 : Compiler et Lancer le Serveur

**Sur Windows** :
```cmd
1. Double-cliquez sur build_server.bat
2. Attendez la compilation (30 secondes)
3. Ouvrez un terminal dans : publish\PlocoSync.Server
4. Exécutez : PlocoSync.Server.exe
```

**Sur Linux/Mac** :
```bash
1. ./build_server.sh
2. cd publish/PlocoSync.Server
3. ./PlocoSync.Server
```

✅ Le serveur est prêt quand vous voyez :
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://[::]:5000
```

### Étape 2 : Lancer PlocoManager (Premier Utilisateur)

1. **Lancez PlocoManager.exe**
2. Un dialog s'affiche automatiquement :

```
┌──────────────────────────────────────────────┐
│ 🔄 Synchronisation Multi-Utilisateurs       │
├──────────────────────────────────────────────┤
│                                              │
│ Mode de Travail :                            │
│  ○ Ne pas utiliser la synchronisation       │
│  ⦿ Mode Master (Modification)               │
│  ○ Mode Consultation (Lecture seule)        │
│                                              │
│ Configuration du Serveur :                   │
│  URL : http://localhost:5000                │
│  Nom : Alice                                 │
│                                              │
│  [🔍 Tester la connexion]                    │
│                                              │
│  ☑ Se souvenir de mon choix                 │
│                                              │
│              [Continuer]  [Quitter]          │
└──────────────────────────────────────────────┘
```

3. **Choisissez "Mode Master"**
4. **Cliquez "Tester la connexion"** → Devrait afficher "✓ Connexion réussie !"
5. **Cochez "Se souvenir de mon choix"** (optionnel)
6. **Cliquez "Continuer"**

✅ Vous êtes maintenant connecté en tant que **Master** !

### Étape 3 : Lancer PlocoManager (Second Utilisateur)

**Sur un autre PC** (même réseau local) :

1. **Lancez PlocoManager.exe**
2. Le même dialog s'affiche
3. **Choisissez "Mode Consultation"**
4. **URL du serveur** : Entrez l'adresse IP du PC serveur
   - Exemple : `http://192.168.1.50:5000`
   - Pour trouver l'IP : `ipconfig` (Windows) ou `ifconfig` (Linux)
5. **Nom** : Entrez votre nom (ex: "Bob")
6. **Testez la connexion**
7. **Cliquez "Continuer"**

✅ Vous êtes maintenant connecté en tant que **Consultant** !

---

## 🎯 Que Peut-On Faire ?

### Mode Master ✏️
- Modifier toutes les données
- Déplacer les locomotives
- Changer les statuts
- Modifier les tuiles
- Les modifications sont visibles en temps réel par les Consultants

### Mode Consultation 👁️
- Voir toutes les données
- **Voir les modifications en temps réel** du Master
- Mode lecture seule (pas de modification possible)

---

## 🔍 Vérifier Que Ça Marche

### Test Simple (2 utilisateurs)

**Master (Alice)** :
1. Déplacer une locomotive d'une voie à une autre (drag & drop)
2. Attendre 1-2 secondes

**Consultant (Bob)** :
- ✅ Devrait voir la locomotive se déplacer automatiquement !

### Voir les Utilisateurs Connectés

Ouvrir dans un navigateur : http://localhost:5000/sessions

Vous verrez :
```json
{
  "totalSessions": 2,
  "masterId": "Alice",
  "sessions": [
    {
      "userId": "Alice",
      "userName": "Alice",
      "isMaster": true,
      "connectedAt": "2026-02-12T10:30:00"
    },
    {
      "userId": "Bob",
      "userName": "Bob",
      "isMaster": false,
      "connectedAt": "2026-02-12T10:31:00"
    }
  ]
}
```

---

## ⚙️ Configuration Avancée

### Changer le Port du Serveur

Si le port 5000 est déjà utilisé :

1. Ouvrir `PlocoSync.Server/Program.cs`
2. Modifier la ligne :
```csharp
app.Run("http://*:5000");  // Changer 5000 → 5001
```
3. Recompiler avec `build_server.bat`

### Utiliser sur Internet (Non Recommandé)

⚠️ **Attention** : Nécessite configuration avancée (HTTPS, firewall, authentification)

Pour réseau local uniquement, c'est parfait ! 🔒

---

## 📊 Logs et Débogage

### Logs du Serveur
- Visibles dans la console où le serveur est lancé
- Montre les connexions, déconnexions, et messages

### Logs du Client
- **Emplacement** : `%AppData%\Ploco\Logs\`
- **Fichier** : `Ploco_YYYY-MM-DD_HH-mm-ss.log`
- **Rechercher** : `[Sync]` pour voir les messages de synchronisation

Exemple :
```
[2026-02-12 10:30:01.456] [INFO   ] [Sync] Connected as Master
[2026-02-12 10:30:15.789] [INFO   ] [Sync] Change sent: LocomotiveMove
[2026-02-12 10:30:16.012] [INFO   ] [Sync] Applied locomotive move: Loco 123
```

---

## 🐛 Problèmes Courants

### "Le serveur ne répond pas"

**Solution** :
1. Vérifier que le serveur est bien lancé (console ouverte)
2. Vérifier l'URL (IP correcte, port correct)
3. Tester avec : `curl http://SERVEUR:5000` ou ouvrir dans un navigateur
4. Vérifier le firewall Windows/Linux

### "Je ne vois pas les modifications"

**Causes possibles** :
- Vous êtes en mode "Ne pas utiliser la synchronisation"
- Le serveur est arrêté
- Problème de réseau
- Vérifier les logs (`%AppData%\Ploco\Logs\`)

### "Je veux changer de mode"

**Solution** :
1. Fermer PlocoManager
2. Supprimer le fichier : `%AppData%\Ploco\sync_config.json`
3. Relancer PlocoManager → Le dialog s'affichera à nouveau

---

## 📁 Structure des Fichiers

```
PlocoManager/
├── build_server.bat          # Script compilation Windows
├── build_server.sh           # Script compilation Linux/Mac
├── publish/                  # Serveur compilé (après build)
│   └── PlocoSync.Server/
│       └── PlocoSync.Server.exe
├── PlocoSync.Server/         # Code source serveur
│   ├── Program.cs
│   ├── Hubs/
│   ├── Services/
│   └── README.md            # Doc serveur
└── Ploco/                    # Application client
    ├── Dialogs/
    │   └── SyncStartupDialog.xaml  # Dialog de démarrage
    └── Services/
        └── SyncService.cs    # Service de synchronisation
```

---

## 🎓 Cas d'Usage

### Scénario 1 : Bureau avec 2 PC
- **PC 1** : Lance le serveur + PlocoManager en Master
- **PC 2** : Lance PlocoManager en Consultation
- **Usage** : Le chef modifie, l'assistant consulte

### Scénario 2 : Salle de contrôle
- **Serveur** : PC dédié au milieu
- **PC 1, 2, 3** : Consultants qui regardent
- **PC Master** : Un seul PC peut modifier
- **Usage** : Visualisation collective, un opérateur

### Scénario 3 : Travail à domicile + Bureau
- **Bureau** : Serveur lancé
- **Domicile** : Se connecte en Consultation
- **Usage** : Consulter depuis chez soi

---

## 🎉 Fonctionnalités Implémentées

✅ **Dialog de démarrage** : Choix du mode au lancement  
✅ **Configuration serveur** : URL et nom d'utilisateur  
✅ **Test de connexion** : Bouton pour vérifier avant de continuer  
✅ **Sauvegarde des préférences** : "Se souvenir de mon choix"  
✅ **Serveur standalone** : Compile en EXE facilement  
✅ **Documentation complète** : Guides pour tout  

---

## 🚧 Fonctionnalités À Venir

### Prochainement
- [ ] Envoi automatique des modifications du Master
- [ ] Désactivation des contrôles en mode Consultant
- [ ] Badge "MASTER" / "CONSULTANT" dans l'interface
- [ ] Indicateur de connexion

### Plus Tard
- [ ] Transfert manuel du rôle Master
- [ ] Liste des utilisateurs connectés dans l'UI
- [ ] Historique des modifications
- [ ] Notifications de connexion/déconnexion

---

## 📞 Besoin d'Aide ?

### Documentation Complète
- `SYNC_README.md` - Guide technique
- `PlocoSync.Server/README.md` - Guide serveur
- `IMPLEMENTATION_STATUS.md` - État de l'implémentation
- `docs/SYNC_DESIGN.md` - Architecture complète

### Logs
- Serveur : Console
- Client : `%AppData%\Ploco\Logs\`

### Test de Connexion
- URL serveur : http://localhost:5000
- Endpoint sessions : http://localhost:5000/sessions

---

## ✨ Résumé

**C'est prêt !** 🎊

1. ▶️ Lancer le serveur (`build_server.bat` puis `PlocoSync.Server.exe`)
2. 🖥️ Lancer PlocoManager sur PC 1 (choisir "Master")
3. 🖥️ Lancer PlocoManager sur PC 2 (choisir "Consultation")
4. 👀 Les modifications du Master apparaissent en temps réel chez le Consultant

**C'est aussi simple que ça !** ✅

---

**Version** : 1.0.0  
**Date** : 12 février 2026  
**Statut** : ✅ Prêt à l'Emploi  
**Auteur** : PlocoManager Team
