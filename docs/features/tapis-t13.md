# TapisT13 - Rapport Intelligent d'État du Parc

## Vue d'ensemble

Le TapisT13 est un rapport intelligent qui affiche l'état complet du parc de locomotives avec un affichage différencié selon le contexte et le statut.

## Caractéristiques Principales

### 1. Support du Placement Prévisionnel

Le rapport prend en compte le mode prévisionnel :
- **Si locomotive en mode prévisionnel** (bleue) : Utilise la position du **ghost** (position future)
- **Sinon** : Utilise la position réelle actuelle

Cela permet de visualiser l'état futur du parc avant de valider les placements.

### 2. Affichage Différencié par Contexte

#### Locomotives HS (Hors Service)
- **Couleur** : 🔴 Rouge
- **Format** : "TileName TrainNumber"
- **Colonnes** : Apparaît dans les **deux colonnes** (rapport + gestion)

#### Locomotives OK/ManqueTraction sur Ligne avec Train
- **Couleur** : 🟢 Vert
- **Format** : "TileName TrainNumber"
- **Colonnes** : Apparaît uniquement dans la **colonne rapport**

#### Locomotives Disponibles (Dépôt/Garage)
- **Couleur** : Pas de couleur
- **Format** : "DISPO TileName"

#### Locomotives sur Ligne de Roulement
- **Couleur** : Pas de couleur
- **Format** : Numéro seul (ex: "1103")

### 3. Pourcentages de Traction

Le rapport inclut maintenant les **pourcentages de traction** :
- **75%** : Léger manque de traction
- **50%** : Traction moyennement réduite
- **25%** : Traction fortement réduite

Ces pourcentages sont affichés à côté du statut, permettant une vision précise de la capacité de traction du parc.

## Logique Technique

### Détermination du Type de Tuile

Le rapport identifie automatiquement le type de tuile :
- **Ligne de roulement** : Tuile avec `RollingLines`
- **Dépôt/Garage** : Autres types de tuiles

### Gestion des Trains

Pour les lignes de roulement, le rapport vérifie :
- Présence d'informations de train (`TrainNumber` non vide)
- Affichage du numéro de train dans le rapport

### Couleurs Conditionnelles

- **Rouge** : Statut HS uniquement
- **Vert** : Locomotive OK ou ManqueTraction sur ligne avec train
- **Pas de couleur** : Autres cas

## Utilisation

### Accès

Menu **Vue > TapisT13** ou raccourci configuré

### Lecture du Rapport

1. **Colonne de gauche (Rapport)** :
   - Locomotives HS (rouge)
   - Locomotives sur ligne avec train (vert)
   - Locomotives disponibles

2. **Colonne de droite (Gestion)** :
   - Locomotives HS (rouge)
   - État général du parc

### Avec Placement Prévisionnel

1. Activez un ou plusieurs placements prévisionnels
2. Ouvrez le TapisT13
3. Les futures positions sont affichées (ghosts)
4. Validez les placements si le résultat vous convient

## Avantages

- ✅ **Vision complète** du parc en un coup d'œil
- ✅ **Anticipation** avec support du placement prévisionnel
- ✅ **Clarté** grâce à l'affichage différencié par couleur
- ✅ **Précision** avec les pourcentages de traction
- ✅ **Cohérence** avec le système existant

## Cas d'Usage

### Planification Quotidienne

1. Consultez le TapisT13 pour voir l'état actuel
2. Utilisez le placement prévisionnel pour planifier
3. Revérifiez le TapisT13 avec les futurs placements
4. Validez si satisfait

### Gestion des HS

- Les locomotives HS apparaissent en rouge dans les deux colonnes
- Vision immédiate des machines hors service
- Facilite la prise de décision pour les affectations

### Suivi de la Traction

- Pourcentages de traction visibles directement
- Permet d'évaluer la capacité globale du parc
- Aide à la décision pour les affectations selon les besoins

## Voir Aussi

- [Placement Prévisionnel](placement-previsionnel.md)
- [Informations de Traction](traction-info.md)
- [Guide Utilisateur](../USER_GUIDE.md)
