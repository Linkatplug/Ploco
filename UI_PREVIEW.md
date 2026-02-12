# 🖼️ Aperçu Visuel - Dialog de Synchronisation

## Dialog au Démarrage

Voici à quoi ressemble le dialog qui s'affiche au démarrage de PlocoManager :

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  Configuration de la Synchronisation                       ┃
┡━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┩
│                                                              │
│  🔄 Synchronisation Multi-Utilisateurs                       │
│                                                              │
│  ─────────────────────────────────────────────────────────  │
│                                                              │
│  La synchronisation permet à plusieurs utilisateurs de       │
│  travailler ensemble sur PlocoManager. Choisissez votre     │
│  mode de travail ci-dessous.                                │
│                                                              │
│  ┌─ Mode de Travail ─────────────────────────────────────┐  │
│  │                                                        │  │
│  │  ○ Ne pas utiliser la synchronisation                 │  │
│  │                                                        │  │
│  │  ⦿ Mode Master (Modification)                         │  │
│  │     Je souhaite modifier les données. Je serai        │  │
│  │     Master ou deviendrai Master si disponible.        │  │
│  │                                                        │  │
│  │  ○ Mode Consultation (Lecture seule)                  │  │
│  │     Je souhaite uniquement consulter. Je verrai       │  │
│  │     les modifications en temps réel.                  │  │
│  │                                                        │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  ┌─ Configuration du Serveur ──────────────────────────────┐│
│  │                                                          ││
│  │  Adresse du serveur de synchronisation :                ││
│  │  ┌────────────────────────────────────────────────────┐ ││
│  │  │ http://localhost:5000                              │ ││
│  │  └────────────────────────────────────────────────────┘ ││
│  │                                                          ││
│  │  Votre nom d'utilisateur :                              ││
│  │  ┌────────────────────────────────────────────────────┐ ││
│  │  │ Alice                                              │ ││
│  │  └────────────────────────────────────────────────────┘ ││
│  │                                                          ││
│  │  [ 🔍 Tester la connexion ]                             ││
│  │  ✓ Connexion réussie !                                  ││
│  │                                                          ││
│  └──────────────────────────────────────────────────────────┘│
│                                                              │
│  ☑ Se souvenir de mon choix au prochain démarrage           │
│                                                              │
│  ─────────────────────────────────────────────────────────  │
│                                                              │
│                                 [ Continuer ]  [ Quitter ]  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## Scénarios d'Utilisation

### Scénario 1 : Travail en Mode Local (Pas de Sync)

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  Mode de Travail :                              ┃
┡━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┩
│  ⦿ Ne pas utiliser la synchronisation          │
│                                                 │
│  ○ Mode Master (Modification)                  │
│  ○ Mode Consultation (Lecture seule)           │
└─────────────────────────────────────────────────┘

→ Configuration du serveur : DÉSACTIVÉE (grisée)
→ Cliquer [Continuer]
→ PlocoManager démarre en mode local classique
```

### Scénario 2 : Premier Utilisateur (Master)

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  Mode de Travail :                              ┃
┡━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┩
│  ○ Ne pas utiliser la synchronisation          │
│                                                 │
│  ⦿ Mode Master (Modification)                  │
│                                                 │
│  ○ Mode Consultation (Lecture seule)           │
└─────────────────────────────────────────────────┘

┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  Configuration du Serveur :                     ┃
┡━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┩
│  URL : http://localhost:5000                    │
│  Nom : Alice                                    │
│                                                 │
│  [ 🔍 Tester la connexion ]                     │
│  ✓ Connexion réussie !                          │
└─────────────────────────────────────────────────┘

→ Cliquer [Continuer]
→ Alice devient Master (premier connecté)
→ Peut modifier toutes les données
```

### Scénario 3 : Second Utilisateur (Consultant)

```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  Mode de Travail :                              ┃
┡━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┩
│  ○ Ne pas utiliser la synchronisation          │
│                                                 │
│  ○ Mode Master (Modification)                  │
│                                                 │
│  ⦿ Mode Consultation (Lecture seule)           │
└─────────────────────────────────────────────────┘

┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃  Configuration du Serveur :                     ┃
┡━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┩
│  URL : http://192.168.1.50:5000                 │
│  Nom : Bob                                      │
│                                                 │
│  [ 🔍 Tester la connexion ]                     │
│  ✓ Connexion réussie !                          │
└─────────────────────────────────────────────────┘

→ Cliquer [Continuer]
→ Bob devient Consultant automatiquement
→ Voit les modifications de Alice en temps réel
```

---

## États du Bouton "Tester la connexion"

### En Cours

```
[ 🔍 Tester la connexion ]
Test en cours...
```

### Succès ✓

```
[ 🔍 Tester la connexion ]
✓ Connexion réussie !  (en vert)
```

### Échec ✗

```
[ 🔍 Tester la connexion ]
✗ Connexion échouée. Vérifiez l'URL du serveur.  (en rouge)
```

### Erreur

```
[ 🔍 Tester la connexion ]
✗ Erreur : Cannot connect to localhost:5000  (en rouge)
```

---

## Option "Se souvenir"

### Case Cochée

```
☑ Se souvenir de mon choix au prochain démarrage
```

**Effet** :
- Configuration sauvegardée dans `%AppData%\Ploco\sync_config.json`
- Au prochain démarrage, les valeurs sont pré-remplies
- Le dialog s'affiche quand même (possibilité de changer)

### Case Non Cochée

```
☐ Se souvenir de mon choix au prochain démarrage
```

**Effet** :
- Aucune sauvegarde
- Au prochain démarrage, valeurs par défaut
- Fichier config supprimé s'il existait

---

## Boutons d'Action

### Bouton "Continuer"

```
[ Continuer ]  (Bouton par défaut, bleu, gras)
```

**Comportement** :
- Valide la configuration
- Vérifie que les champs obligatoires sont remplis
- Sauvegarde si "Se souvenir" est coché
- Ferme le dialog
- Lance PlocoManager avec la config choisie

### Bouton "Quitter"

```
[ Quitter ]  (Bouton secondaire, gris)
```

**Comportement** :
- Ferme le dialog
- **Ferme l'application PlocoManager**
- Aucune sauvegarde effectuée

---

## Messages d'Erreur

### Champs Vides (Mode Sync Activé)

Si vous choisissez Master ou Consultation sans remplir les champs :

```
┌─────────────────────────────────────┐
│  ⚠ Validation                       │
├─────────────────────────────────────┤
│                                     │
│  Veuillez saisir l'adresse du       │
│  serveur.                           │
│                                     │
│           [ OK ]                    │
│                                     │
└─────────────────────────────────────┘
```

ou

```
┌─────────────────────────────────────┐
│  ⚠ Validation                       │
├─────────────────────────────────────┤
│                                     │
│  Veuillez saisir votre nom          │
│  d'utilisateur.                     │
│                                     │
│           [ OK ]                    │
│                                     │
└─────────────────────────────────────┘
```

---

## Disposition Responsive

Le dialog a une taille fixe pour une expérience cohérente :

- **Largeur** : 540 pixels
- **Hauteur** : 480 pixels
- **Position** : Centre de l'écran
- **Redimensionnement** : Désactivé (ResizeMode="NoResize")
- **Style** : Bordure simple (SingleBorderWindow)

---

## Codes Couleurs

### Feedback Visuel

- **Texte normal** : Noir (#000000)
- **Texte secondaire** : Gris (#666666)
- **Connexion réussie** : Vert (Colors.Green)
- **Connexion échouée** : Rouge (Colors.Red)
- **Test en cours** : Gris (Colors.Gray)
- **Fond panneau** : Gris clair (#F9F9F9)

---

## Workflow Visuel

```
┌─────────────────┐
│  PlocoManager   │
│  démarre        │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Dialog de      │◄─── Affichage automatique
│  Synchronisation│
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Utilisateur    │
│  choisit mode   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Configure URL  │
│  + Nom          │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Teste la       │
│  connexion      │
└────────┬────────┘
         │
         ├─ Succès ─►┌─────────────────┐
         │           │  Coche "Se      │
         │           │  souvenir"?     │
         │           └────────┬────────┘
         │                    │
         │                    ▼
         │           ┌─────────────────┐
         │           │  [Continuer]    │
         │           └────────┬────────┘
         │                    │
         │                    ▼
         │           ┌─────────────────┐
         │           │  PlocoManager   │
         │           │  démarre avec   │
         │           │  la config      │
         │           └─────────────────┘
         │
         └─ Échec ──►┌─────────────────┐
                     │  Message        │
                     │  d'erreur       │
                     │  [Réessayer]    │
                     └─────────────────┘
```

---

## Fichier de Configuration Sauvegardé

**Emplacement** : `%AppData%\Ploco\sync_config.json`

**Contenu** (exemple) :
```json
{
  "Mode": 1,
  "ServerUrl": "http://192.168.1.50:5000",
  "UserName": "Alice",
  "RememberChoice": true
}
```

**Mode** :
- 0 = Disabled (Pas de sync)
- 1 = Master
- 2 = Consultant

---

## Raccourcis Clavier

- **Entrée** : Appuie sur [Continuer] (IsDefault="True")
- **Échap** : Appuie sur [Quitter] (IsCancel="True")
- **Tab** : Navigation entre les champs

---

## Accessibilité

- **Tooltips** : Descriptions sur les champs
- **Tab order** : Navigation logique
- **Labels** : Texte explicite
- **Feedback visuel** : Couleurs + texte
- **Taille de police** : Lisible (11-18pt)

---

**Ce dialog offre une expérience utilisateur complète et intuitive !** ✨
