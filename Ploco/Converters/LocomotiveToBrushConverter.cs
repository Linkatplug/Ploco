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
            // Si la locomotive est dans la tuile d'origine (placement prévisionnel actif), elle est bleue
            if (loco.IsProvisionalPlacement)
            {
                return Brushes.Blue;
            }

            // Si parameter est "provisional", c'est la copie provisionnelle sur la ligne de roulement
            // Elle doit être verte
            if (parameter is string param && param == "provisional")
            {
                return Brushes.Green;
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
