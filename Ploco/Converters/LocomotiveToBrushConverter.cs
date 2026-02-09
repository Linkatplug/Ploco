using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Ploco.Models;

namespace Ploco.Converters
{
    /// <summary>
    /// Converter that returns the appropriate color for a locomotive based on its status and forecast state.
    /// This supports the "Placement prévisionnel" feature.
    /// </summary>
    public class LocomotiveToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LocomotiveModel loco)
            {
                // Forecast states take priority over status colors
                if (loco.IsForecastOrigin)
                {
                    return Brushes.Blue; // Blue in origin tile
                }

                if (loco.IsForecastGhost)
                {
                    return Brushes.Green; // Green ghost on rolling line
                }

                // Otherwise, use the normal status-based color
                return loco.Status switch
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
