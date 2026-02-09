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
        // Forecast color constants
        private static readonly Brush ForecastOriginColor = Brushes.Blue;  // Color for locomotive in origin tile during forecast
        private static readonly Brush ForecastGhostColor = Brushes.Green;  // Color for ghost on target rolling line

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LocomotiveModel loco)
            {
                // Forecast states take priority over status colors
                if (loco.IsForecastOrigin)
                {
                    return ForecastOriginColor;
                }

                if (loco.IsForecastGhost)
                {
                    return ForecastGhostColor;
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
