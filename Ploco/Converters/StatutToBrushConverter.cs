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
            if (value is StatutLocomotive statut)
            {
                return statut switch
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
