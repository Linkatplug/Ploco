# 🔴 PROBLÈMES CRITIQUES DE SYNCHRONISATION

**Date**: 12 février 2026  
**Priorité**: CRITIQUE  
**Status**: NON RÉSOLU - NÉCESSITE IMPLÉMENTATION

---

## Résumé Exécutif

Les fonctionnalités de synchronisation ont des **problèmes critiques** qui empêchent une collaboration multi-utilisateur correcte :

1. ❌ **Mode Master** : Charge/sauvegarde en LOCAL au lieu du SERVEUR
2. ❌ **Mode Consultation** : Charge en LOCAL au lieu du SERVEUR (pas un vrai miroir)
3. ❌ **Mode Consultation** : L'utilisateur PEUT MODIFIER (pas lecture seule)
4. ❌ **Pas de notification** : "Aucun état sur le serveur" n'apparaît jamais

---

## Problèmes Détaillés

### 1. Mode Master - Charge/Sauvegarde Local ❌

**Comportement Actuel**:
```
PC1 (Master) se connecte au serveur
→ Charge depuis ploco.db LOCAL
→ Sauvegarde vers ploco.db LOCAL
→ Barre de statut : "Dernière sauvegarde : 14:32:15 (Local)"
```

**Comportement Attendu**:
```
PC1 (Master) se connecte au serveur
→ Demande l'état au serveur (GetState)
→ Si état existe : télécharge shared_ploco.db et charge
→ Si état n'existe pas : affiche "Aucun état sur le serveur, démarrage vierge"
→ À chaque modification : sauvegarde vers le serveur (SaveState)
→ Barre de statut : "Dernière sauvegarde : 14:32:15 (Serveur)"
```

**Impact**:
- Master travaille sur données locales obsolètes
- Pas de partage d'état entre sessions
- Consultant ne peut pas voir l'état Master

---

### 2. Mode Consultation - Charge Local ❌

**Comportement Actuel**:
```
PC2 (Consultation) se connecte au serveur
→ Charge depuis ploco.db LOCAL (ses propres données)
→ Voit SON état local, pas celui du Master
→ N'est PAS un miroir du Master
```

**Comportement Attendu**:
```
PC2 (Consultation) se connecte au serveur
→ Demande l'état au serveur (GetState)
→ Télécharge shared_ploco.db du Master
→ Charge l'état du serveur
→ Voit EXACTEMENT ce que voit le Master (vrai miroir)
→ Reçoit les mises à jour en temps réel
```

**Impact**:
- Consultant voit ses propres données, pas celles du Master
- Pas de collaboration réelle
- Mode Consultation inutile

---

### 3. Mode Consultation - Pas Lecture Seule ❌

**Comportement Actuel**:
```
PC2 (Consultation) est connecté
→ Peut déplacer les locomotives (drag & drop)
→ Peut modifier les statuts
→ Peut déplacer/redimensionner les tuiles
→ Peut ajouter/supprimer des lieux
→ Toutes les actions fonctionnent normalement
```

**Comportement Attendu**:
```
PC2 (Consultation) est connecté
→ TOUS les contrôles sont désactivés
→ NE PEUT PAS déplacer les locomotives
→ NE PEUT PAS modifier les statuts
→ NE PEUT PAS déplacer/redimensionner les tuiles
→ NE PEUT PAS ajouter/supprimer des lieux
→ Mode LECTURE SEULE stricte
→ Indicateur visuel : "MODE CONSULTATION - LECTURE SEULE"
```

**Impact**:
- Consultant peut modifier = chaos dans les données
- Pas de vrai mode "observation"
- Risque de conflits et pertes de données

---

### 4. Pas de Notification "No State" ❌

**Comportement Actuel**:
```
PC1 (Master) se connecte à un serveur vierge
→ Pas de message
→ Charge ploco.db local
→ Continue normalement
```

**Comportement Attendu**:
```
PC1 (Master) se connecte à un serveur vierge
→ Appelle GetState() → retourne null
→ Affiche dialog :
   "Aucun état trouvé sur le serveur.
    Démarrage avec une base vide.
    [OK]"
→ Réinitialise : tuiles, locomotives, historique
→ Démarre proprement
```

**Impact**:
- Confusion sur l'état de synchronisation
- Données obsolètes chargées
- Pas de démarrage propre

---

## Cause Racine

### Phase 3 Jamais Implémentée ❌

La **Phase 3** (Server State Load/Save) n'a JAMAIS été implémentée :

**Côté Serveur** - MANQUANT :
- ❌ Pas d'endpoint `GetState()`
- ❌ Pas d'endpoint `SaveState()`
- ❌ Pas de stockage de fichier DB
- ❌ Pas de gestion de metadata

**Côté Client** - MANQUANT :
- ❌ Pas d'appel à `GetState()` au démarrage
- ❌ Pas d'appel à `SaveState()` sur modification
- ❌ Pas de notification "no state"
- ❌ Pas de désactivation des contrôles en Consultation

**Ce Qui Fonctionne** ✅ :
- Connexion au serveur
- Assignation des rôles (Master/Consultant)
- Sync temps réel des CHANGEMENTS (moves, status, tiles)
- Barre de statut UI
- Heartbeat

**Ce Qui Ne Fonctionne PAS** ❌ :
- Chargement de l'état depuis le serveur
- Sauvegarde de l'état vers le serveur
- Mode lecture seule en Consultation

---

## Solution Requise

### 1. Endpoints Serveur (CRITIQUE) 🔴

**Fichier**: `PlocoSync.Server/Hubs/PlocoSyncHub.cs`

```csharp
// Nouveau : GetState
public async Task<byte[]?> GetState()
{
    var path = Path.Combine(_stateStoragePath, "shared_ploco.db");
    if (!File.Exists(path))
        return null;
    
    return await File.ReadAllBytesAsync(path);
}

// Nouveau : SaveState
public async Task SaveState(byte[] dbBytes)
{
    var path = Path.Combine(_stateStoragePath, "shared_ploco.db");
    await File.WriteAllBytesAsync(path, dbBytes);
    
    // Save metadata
    var metadata = new 
    {
        LastSaved = DateTime.UtcNow,
        SavedBy = Context.ConnectionId,
        UserName = /* get from session */
    };
    
    var metaPath = Path.Combine(_stateStoragePath, "state_metadata.json");
    await File.WriteAllTextAsync(metaPath, JsonSerializer.Serialize(metadata));
}
```

**Configuration** :
- Ajouter `StateStoragePath` dans appsettings.json
- Créer dossier de stockage au démarrage
- Injecter configuration dans Hub

---

### 2. Client Load State (CRITIQUE) 🔴

**Fichier**: `Ploco/MainWindow.xaml.cs`

```csharp
private async Task LoadStateFromServerAsync()
{
    if (_syncService == null || !_syncService.IsConnected)
        return;
    
    Logger.Info("Loading state from server...", "Sync");
    
    try
    {
        // Demander l'état au serveur
        var stateBytes = await _syncService.GetStateAsync();
        
        if (stateBytes == null)
        {
            // Aucun état sur le serveur
            if (_syncService.IsMaster)
            {
                // Afficher message uniquement pour Master
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show(
                        "Aucun état trouvé sur le serveur.\n" +
                        "Démarrage avec une base vide.",
                        "Synchronisation",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                });
                
                // Réinitialiser la base locale
                await ResetLocalDatabaseAsync();
            }
            
            Logger.Info("No state on server, starting fresh", "Sync");
            return;
        }
        
        // Écrire le fichier DB local
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ploco.db");
        await File.WriteAllBytesAsync(dbPath, stateBytes);
        
        Logger.Info($"State loaded from server ({stateBytes.Length} bytes)", "Sync");
        
        // Recharger les données
        await ReloadDataFromDatabaseAsync();
        
        // Mettre à jour la barre de statut
        UpdateLastSaveTime(true); // true = serveur
    }
    catch (Exception ex)
    {
        Logger.Error($"Failed to load state: {ex.Message}", "Sync");
        MessageBox.Show(
            $"Erreur lors du chargement de l'état : {ex.Message}",
            "Erreur",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
}

private async Task ReloadDataFromDatabaseAsync()
{
    // Recharger locomotives
    _locomotives.Clear();
    var locos = await _db.GetAllLocomotivesAsync();
    foreach (var loco in locos)
        _locomotives.Add(loco);
    
    // Recharger tiles
    _tiles.Clear();
    var tiles = await _db.GetAllTilesAsync();
    foreach (var tile in tiles)
        _tiles.Add(tile);
    
    // Recharger historique
    await RefreshHistoryAsync();
    
    Logger.Info("Data reloaded from database", "Sync");
}

private async Task ResetLocalDatabaseAsync()
{
    // Supprimer toutes les données
    await _db.DeleteAllLocomotivesAsync();
    await _db.DeleteAllTilesAsync();
    await _db.DeleteAllHistoryAsync();
    
    _locomotives.Clear();
    _tiles.Clear();
    
    Logger.Info("Local database reset", "Sync");
}
```

**Appel au démarrage** :
```csharp
private async Task InitializeSyncService()
{
    // ... code existant de connexion ...
    
    if (dialogResult == true && config != null)
    {
        // ... créer SyncService ...
        
        await _syncService.ConnectAsync();
        
        // NOUVEAU : Charger l'état depuis le serveur
        if (config.Enabled && _syncService.IsConnected)
        {
            await LoadStateFromServerAsync();
        }
        
        UpdateStatusBar();
    }
}
```

---

### 3. Client Save State (CRITIQUE) 🔴

**Fichier**: `Ploco/Services/SyncService.cs`

```csharp
public async Task<byte[]?> GetStateAsync()
{
    if (_connection == null || _connection.State != HubConnectionState.Connected)
        return null;
    
    try
    {
        return await _connection.InvokeAsync<byte[]?>("GetState");
    }
    catch (Exception ex)
    {
        Logger.Error($"GetState failed: {ex.Message}");
        return null;
    }
}

public async Task SaveStateAsync(byte[] dbBytes)
{
    if (_connection == null || _connection.State != HubConnectionState.Connected)
        return;
    
    if (!IsMaster)
    {
        Logger.Warning("Cannot save state - not Master");
        return;
    }
    
    try
    {
        await _connection.InvokeAsync("SaveState", dbBytes);
        Logger.Info($"State saved to server ({dbBytes.Length} bytes)");
    }
    catch (Exception ex)
    {
        Logger.Error($"SaveState failed: {ex.Message}");
    }
}
```

**Fichier**: `Ploco/MainWindow.xaml.cs`

```csharp
private Timer? _saveTimer;
private const int SAVE_DEBOUNCE_MS = 800;

private void ScheduleServerSave()
{
    if (_syncService == null || !_syncService.IsMaster)
        return;
    
    // Annuler le timer précédent
    _saveTimer?.Stop();
    _saveTimer?.Dispose();
    
    // Nouveau timer
    _saveTimer = new Timer(SAVE_DEBOUNCE_MS);
    _saveTimer.Elapsed += async (s, e) =>
    {
        await SaveStateToServerAsync();
    };
    _saveTimer.AutoReset = false;
    _saveTimer.Start();
}

private async Task SaveStateToServerAsync()
{
    if (_syncService == null || !_syncService.IsConnected || !_syncService.IsMaster)
        return;
    
    try
    {
        // Lire le fichier DB local
        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ploco.db");
        var dbBytes = await File.ReadAllBytesAsync(dbPath);
        
        // Envoyer au serveur
        await _syncService.SaveStateAsync(dbBytes);
        
        // Mettre à jour la barre de statut
        Dispatcher.Invoke(() => UpdateLastSaveTime(true)); // true = serveur
        
        Logger.Info("State saved to server", "Sync");
    }
    catch (Exception ex)
    {
        Logger.Error($"Failed to save state to server: {ex.Message}", "Sync");
    }
}
```

**Appeler après chaque modification** :
```csharp
private void Locomotive_Drop(...)
{
    // ... code existant ...
    
    PersistState(); // Sauvegarde locale
    
    // NOUVEAU : Planifier sauvegarde serveur
    if (_syncService?.IsMaster == true)
    {
        ScheduleServerSave();
    }
}
```

---

### 4. Mode Consultation Lecture Seule (CRITIQUE) 🔴

**Fichier**: `Ploco/MainWindow.xaml.cs`

```csharp
private void UpdateConsultantMode()
{
    bool isConsultant = _syncService?.IsConnected == true && 
                        !_syncService.IsMaster;
    
    if (isConsultant)
    {
        // Désactiver TOUS les contrôles de modification
        DisableAllEditControls();
        ShowConsultantBanner();
    }
    else
    {
        // Réactiver les contrôles
        EnableAllEditControls();
        HideConsultantBanner();
    }
}

private void DisableAllEditControls()
{
    // Désactiver drag & drop locomotives
    foreach (var loco in _locomotives)
    {
        // Retirer les event handlers de drag
        var locoControl = FindLocoControl(loco);
        if (locoControl != null)
        {
            locoControl.AllowDrop = false;
            locoControl.IsEnabled = false; // Ou utiliser un flag custom
        }
    }
    
    // Désactiver drag & drop tiles
    foreach (var tile in _tiles)
    {
        var tileControl = FindTileControl(tile);
        if (tileControl != null)
        {
            tileControl.AllowDrop = false;
            tileControl.IsEnabled = false;
        }
    }
    
    // Désactiver boutons
    BtnAddLocation.IsEnabled = false;
    
    // Désactiver menus contextuels
    // ... tous les menus de modification
    
    Logger.Info("Consultant mode - all controls disabled", "UI");
}

private void EnableAllEditControls()
{
    // Réactiver tout
    BtnAddLocation.IsEnabled = true;
    // ... etc
    
    Logger.Info("Edit mode - all controls enabled", "UI");
}

private void ShowConsultantBanner()
{
    // Ajouter un bandeau en haut
    var banner = new Border
    {
        Background = new SolidColorBrush(Color.FromRgb(255, 200, 0)),
        Padding = new Thickness(10),
        Child = new TextBlock
        {
            Text = "MODE CONSULTATION - LECTURE SEULE",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            HorizontalAlignment = HorizontalAlignment.Center
        }
    };
    
    // Insérer en haut du layout principal
    // MainGrid.Children.Insert(0, banner);
}
```

**Appeler quand le statut change** :
```csharp
private void UpdateMasterStatus(bool isMaster)
{
    _isMaster = isMaster;
    Logger.Info($"Master status changed: {isMaster}", "Sync");
    
    UpdateStatusBar();
    UpdateConsultantMode(); // NOUVEAU
}
```

---

## Estimation de Travail

### Implémentation Complète

| Tâche | Temps | Priorité |
|-------|-------|----------|
| Endpoints serveur | 2-3h | CRITIQUE 🔴 |
| Client load state | 2-3h | CRITIQUE 🔴 |
| Client save state | 2-3h | CRITIQUE 🔴 |
| Consultant read-only | 1-2h | CRITIQUE 🔴 |
| Tests & debugging | 2-3h | CRITIQUE 🔴 |
| **TOTAL** | **9-14h** | **CRITIQUE** |

### Ordre d'Implémentation

1. ✅ **Serveur** (endpoints GetState/SaveState) - DOIT être fait en premier
2. ✅ **Client Load** (charger depuis serveur au démarrage)
3. ✅ **Client Save** (sauvegarder vers serveur avec debounce)
4. ✅ **Consultant UI** (désactiver tous les contrôles)
5. ✅ **Tests** (vérifier tous les scénarios)

---

## Tests Requis

### Scénario 1 : Master Fresh Start
```
1. Serveur vierge (pas de shared_ploco.db)
2. PC1 se connecte en Master
3. ✅ Vérifie : Message "Aucun état sur le serveur"
4. ✅ Vérifie : Base vide (pas de locos/tiles)
5. Ajouter une locomotive
6. ✅ Vérifie : Sauvegarde vers serveur
7. ✅ Vérifie : Barre de statut = "Serveur"
```

### Scénario 2 : Master Load Existing
```
1. Serveur a shared_ploco.db (état existant)
2. PC1 se connecte en Master
3. ✅ Vérifie : Charge l'état depuis le serveur
4. ✅ Vérifie : Voit les locomotives/tiles du serveur
5. ✅ Vérifie : Pas de message "no state"
6. Modifier une locomotive
7. ✅ Vérifie : Sauvegarde vers serveur (après 800ms)
```

### Scénario 3 : Consultant Mirror
```
1. Master est connecté avec des données
2. PC2 se connecte en Consultation
3. ✅ Vérifie : Charge l'état depuis le serveur
4. ✅ Vérifie : Voit EXACTEMENT les mêmes données que Master
5. Master déplace une locomotive
6. ✅ Vérifie : Consultant voit le changement en temps réel
7. ✅ Vérifie : Barre de statut Consultant = "Serveur"
```

### Scénario 4 : Consultant Read-Only
```
1. PC2 connecté en Consultation
2. ✅ Vérifie : Bandeau "LECTURE SEULE" visible
3. ✅ Vérifie : Cannot drag locomotives
4. ✅ Vérifie : Cannot modify status
5. ✅ Vérifie : Cannot move/resize tiles
6. ✅ Vérifie : "Ajouter un lieu" désactivé
7. ✅ Vérifie : Tous menus contextuels désactivés
```

---

## Statut Actuel

### ✅ Ce Qui Fonctionne (Phase 1 & 2)
- Shutdown propre
- Barre de statut UI
- Sélection de mode au démarrage
- Connexion au serveur
- Assignation Master/Consultant
- Sync temps réel des CHANGEMENTS (moves, status, tiles)
- Heartbeat

### ❌ Ce Qui Ne Fonctionne PAS (Phase 3)
- Chargement de l'état depuis le serveur
- Sauvegarde de l'état vers le serveur
- Notification "no state"
- Mode Consultant lecture seule

### Pourcentage Global
- **Phase 1 & 2** : ✅ 100% (shutdown + UI)
- **Phase 3** : ❌ 0% (state load/save + read-only)
- **Global** : 🟡 65%

---

## Recommandation

### PRIORITÉ IMMÉDIATE 🔴

**Ces fonctionnalités sont CRITIQUES pour une utilisation réelle** :

1. 🔴 Implémenter endpoints serveur (GetState/SaveState)
2. 🔴 Implémenter client load/save
3. 🔴 Implémenter Consultant read-only
4. 🔴 Tester tous les scénarios

**Sans Phase 3, le système de synchronisation est INUTILISABLE** :
- Master ne synchronise pas vraiment
- Consultant ne voit pas les données Master
- Consultant peut modifier (chaos)
- Aucune collaboration réelle possible

### Action Immédiate

**NE PAS** déployer en production sans Phase 3 !  
**NE PAS** utiliser en mode multi-utilisateur !  

**IMPLÉMENTER Phase 3 avant toute utilisation réelle !**

---

**Date de ce rapport** : 12 février 2026  
**Urgence** : CRITIQUE 🔴  
**Temps requis** : 9-14 heures  
**Blocage** : Collaboration multi-utilisateur impossible

🔴 **ACTION REQUISE IMMÉDIATEMENT** 🔴
