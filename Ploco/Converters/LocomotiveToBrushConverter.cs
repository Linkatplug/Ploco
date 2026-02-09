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
            if (value is not LocomotiveModel loco)
            {
                return Brushes.Gray;
            }

            // Gestion du placement prévisionnel
            if (loco.IsProvisionalPlacement)
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

            // Sinon, utiliser la couleur basée sur le statut
            return loco.Status switch
            {
                LocomotiveStatus.Ok => Brushes.Green,
                LocomotiveStatus.ManqueTraction => Brushes.Orange,
                LocomotiveStatus.HS => Brushes.Red,
                _ => Brushes.Gray,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
