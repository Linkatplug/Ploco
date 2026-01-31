using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Ploco.Models;

namespace Ploco.Converters
{
    public class StatutToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LocomotiveStatus statut)
            {
                return statut switch
                {
                    LocomotiveStatus.Ok => Brushes.Green,
                    LocomotiveStatus.DefautMineur => Brushes.Gold,
                    LocomotiveStatus.AControler => Brushes.Orange,
                    LocomotiveStatus.HS => Brushes.Red,
                    _ => Brushes.Gray,
                };
            }

            if (value is StatutLocomotive legacyStatut)
            {
                return legacyStatut switch
                {
                    StatutLocomotive.Ok => Brushes.Green,
                    StatutLocomotive.DefautMineur => Brushes.Gold,
                    StatutLocomotive.AControler => Brushes.Orange,
                    StatutLocomotive.HS => Brushes.Red,
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
