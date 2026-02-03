using System.Windows;
using System.Windows.Controls;
using Ploco.Models;

namespace Ploco.Helpers
{
    public class TileTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? DepotTemplate { get; set; }
        public DataTemplate? GarageTemplate { get; set; }
        public DataTemplate? LineTemplate { get; set; }
        public DataTemplate? RollingLineTemplate { get; set; }

        public override DataTemplate? SelectTemplate(object item, DependencyObject container)
        {
            if (item is not TileModel tile)
            {
                return base.SelectTemplate(item, container);
            }

            return tile.Type switch
            {
                TileType.Depot => DepotTemplate,
                TileType.VoieGarage => GarageTemplate,
                TileType.ArretLigne => LineTemplate,
                TileType.LigneRoulement => RollingLineTemplate,
                _ => base.SelectTemplate(item, container)
            };
        }
    }
}
