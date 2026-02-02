using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Ploco.Models;

namespace Ploco.Helpers
{
    public static class ContextMenuHelper
    {
        /// <summary>
        /// Gère le clic droit pour échanger (swap) deux locomotives.
        /// </summary>
        /// <param name="sender">L'objet sender (MenuItem)</param>
        /// <param name="lineasPool">La collection des locomotives du pool Lineas.</param>
        /// <param name="sibelitPool">La collection des locomotives du pool Sibelit.</param>
        /// <param name="findCanvasItemForLoco">
        /// Une fonction permettant de retrouver l'élément Border associé à une locomotive sur le Canvas.
        /// Si aucun Canvas n'est utilisé, on peut passer null.
        /// </param>
        /// <param name="updateInfoZone">
        /// Une action à appeler pour mettre à jour l'interface après l'échange.
        /// </param>
        public static void HandleSwap(object sender,
                              ObservableCollection<LocomotiveModel> lineasPool,
                              ObservableCollection<LocomotiveModel> sibelitPool,
                              Func<LocomotiveModel, Border>? findCanvasItemForLoco,
                              Action? updateInfoZone)
        {
            if (sender is not MenuItem menuItem)
            {
                return;
            }

            if (menuItem.Parent is not ContextMenu contextMenu)
            {
                return;
            }

            if (contextMenu.PlacementTarget is not Border border)
            {
                return;
            }

            var locoFromSibelit = border.DataContext as LocomotiveModel ?? border.Tag as LocomotiveModel;
            if (locoFromSibelit == null)
            {
                return;
            }

            var dialog = new SwapDialog(locoFromSibelit, lineasPool);
            if (dialog.ShowDialog() != true || dialog.SelectedLoco == null)
            {
                return;
            }

            var locoFromLineas = dialog.SelectedLoco;

            if (findCanvasItemForLoco != null)
            {
                var canvasItem = findCanvasItemForLoco(locoFromSibelit);
                if (canvasItem?.Parent is Canvas canvas)
                {
                    canvas.Children.Remove(canvasItem);
                }
            }

            if (sibelitPool.Contains(locoFromSibelit))
            {
                sibelitPool.Remove(locoFromSibelit);
            }
            if (!lineasPool.Contains(locoFromSibelit))
            {
                lineasPool.Add(locoFromSibelit);
            }

            if (lineasPool.Contains(locoFromLineas))
            {
                lineasPool.Remove(locoFromLineas);
            }
            if (!sibelitPool.Contains(locoFromLineas))
            {
                sibelitPool.Add(locoFromLineas);
            }

            locoFromSibelit.Pool = "Lineas";
            locoFromLineas.Pool = "Sibelit";

            updateInfoZone?.Invoke();
        }

        /// <summary>
        /// Gère le clic droit pour modifier le statut d'une locomotive.
        /// </summary>
        /// <param name="sender">L'objet sender (MenuItem)</param>
        /// <param name="updateInfoZone">
        /// Une action à appeler pour mettre à jour l'interface après modification du statut.
        /// </param>
        public static void HandleModifierStatut(object sender, Action? updateInfoZone)
        {
            if (sender is not MenuItem menuItem)
            {
                return;
            }

            if (menuItem.Parent is not ContextMenu contextMenu)
            {
                return;
            }

            if (contextMenu.PlacementTarget is not Border border)
            {
                return;
            }

            var loco = border.DataContext as LocomotiveModel ?? border.Tag as LocomotiveModel;
            if (loco == null)
            {
                return;
            }

            var dialog = new StatusDialog(loco);
            if (dialog.ShowDialog() == true)
            {
                updateInfoZone?.Invoke();
            }
        }

        /// <summary>
        /// Gère le clic droit pour afficher l'historique de modification d'une locomotive.
        /// </summary>
        public static void HandleVoirHistorique(object sender, Window owner)
        {
            MessageBox.Show("L'historique est disponible dans la fenêtre principale.", "Historique",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
