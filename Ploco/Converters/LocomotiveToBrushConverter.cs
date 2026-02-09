using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Ploco.Models;

namespace Ploco.Converters
{
    /// <summary>
    /// Convertisseur pour déterminer la couleur d'une locomotive en fonction de son statut
    /// et de son état de placement prévisionnel.
    /// </summary>
    public class LocomotiveToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Note: We bind to Status property, but the value passed is the Status enum
            // We need the whole locomotive object to check IsProvisionalPlacement
            // So we need to change how this converter is used
            
            // For now, if value is a LocomotiveModel, use it directly
            // If value is a LocomotiveStatus, we can't access provisional placement status
            if (value is LocomotiveModel loco)
            {
                // Priorité 1: Statuts critiques (HS et ManqueTraction) - toujours afficher en rouge/orange
                if (loco.Status == LocomotiveStatus.HS)
                {
                    return Brushes.Red;
                }
                if (loco.Status == LocomotiveStatus.ManqueTraction)
                {
                    return Brushes.Orange;
                }

                // Priorité 2: Gestion du placement prévisionnel (seulement si statut OK)
                if (loco.IsProvisionalPlacement && loco.Status == LocomotiveStatus.Ok)
                {
                    // Si on est dans une rolling line (paramètre = "rollingline"), 
                    // afficher en vert (c'est l'affichage provisionnel)
                    if (parameter is string param && param == "rollingline")
                    {
                        return Brushes.Green;
                    }
                    // Sinon, afficher en bleu (c'est la locomotive dans sa tuile d'origine)
                    return Brushes.Blue;
                }

                // Priorité 3: Statut normal (OK)
                return loco.Status == LocomotiveStatus.Ok ? Brushes.Green : Brushes.Gray;
            }

            // Fallback for when binding to Status enum directly (old behavior)
            if (value is LocomotiveStatus status)
            {
                return status switch
                {
                    LocomotiveStatus.Ok => Brushes.Green,
                    LocomotiveStatus.ManqueTraction => Brushes.Orange,
                    LocomotiveStatus.HS => Brushes.Red,
                    _ => Brushes.Gray,
                };
            }

            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
