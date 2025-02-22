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
        /// Gère le clic droit pour le swap des locomotives.
        /// </summary>
        /// <param name="sender">L'objet sender (MenuItem)</param>
        /// <param name="lineasPool">La collection de locomotives du pool Lineas.</param>
        /// <param name="sibelitPool">La collection de locomotives du pool Sibelit.</param>
        /// <param name="canvas">Le Canvas dans lequel chercher/supprimer l'élément visuel.</param>
        /// <param name="findCanvasItemForLoco">
        /// Une fonction permettant de retrouver l'élément Border associé à une locomotive.
        /// </param>
        /// <param name="updateInfoZone">
        /// Une action à appeler pour mettre à jour la zone d'information après un swap.
        /// </param>
        public static void HandleSwap(object sender, ObservableCollection<Locomotive> lineasPool,
                                      ObservableCollection<Locomotive> sibelitPool,
                                      Canvas canvas,
                                      Func<Locomotive, Border> findCanvasItemForLoco,
                                      Action updateInfoZone)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
                return;

            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu == null)
                return;

            // Le PlacementTarget du ContextMenu est l'élément visuel sur lequel on a cliqué.
            Border border = contextMenu.PlacementTarget as Border;
            if (border == null)
                return;

            // Récupérer la locomotive depuis DataContext ou Tag
            Locomotive locoFromSibelit = border.DataContext as Locomotive ?? border.Tag as Locomotive;
            if (locoFromSibelit == null)
                return;

            // Ouvrir la fenêtre de swap en passant la loco et le pool Lineas.
            SwapDialog dialog = new SwapDialog(locoFromSibelit, lineasPool);
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                // Récupérer la locomotive sélectionnée dans le dialog.
                Locomotive locoFromLineas = dialog.SelectedLoco;
                if (locoFromLineas != null)
                {
                    // Si la loco est sur le Canvas, la supprimer.
                    Border canvasItem = findCanvasItemForLoco(locoFromSibelit);
                    if (canvasItem != null)
                    {
                        canvas.Children.Remove(canvasItem);
                        locoFromSibelit.IsOnCanvas = false;
                    }

                    // Échanger les locomotives entre les pools.
                    if (sibelitPool.Contains(locoFromSibelit))
                        sibelitPool.Remove(locoFromSibelit);
                    if (!lineasPool.Contains(locoFromSibelit))
                        lineasPool.Add(locoFromSibelit);

                    if (lineasPool.Contains(locoFromLineas))
                        lineasPool.Remove(locoFromLineas);
                    if (!sibelitPool.Contains(locoFromLineas))
                        sibelitPool.Add(locoFromLineas);

                    updateInfoZone?.Invoke();
                }
            }
        }

        /// <summary>
        /// Gère le clic droit pour modifier le statut d'une locomotive.
        /// </summary>
        /// <param name="sender">L'objet sender (MenuItem)</param>
        /// <param name="updateInfoZone">
        /// Une action à appeler pour mettre à jour la zone d'information après modification.
        /// </param>
        public static void HandleModifierStatut(object sender, Action updateInfoZone)
        {
            MenuItem menuItem = sender as MenuItem;
            if (menuItem == null)
                return;

            ContextMenu contextMenu = menuItem.Parent as ContextMenu;
            if (contextMenu == null)
                return;

            Border border = contextMenu.PlacementTarget as Border;
            if (border == null)
                return;

            Locomotive loco = border.DataContext as Locomotive ?? border.Tag as Locomotive;
            if (loco == null)
                return;

            ModifierStatutDialog dialog = new ModifierStatutDialog(loco);
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                updateInfoZone?.Invoke();
            }
        }
    }
}
