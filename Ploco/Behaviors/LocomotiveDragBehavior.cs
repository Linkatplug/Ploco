using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Ploco.Models;

namespace Ploco.Behaviors
{
    public class LocomotiveDragBehavior : Behavior<FrameworkElement>
    {
        private Point _dragStartPoint;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove += OnPreviewMouseMove;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
            AssociatedObject.PreviewMouseMove -= OnPreviewMouseMove;
            base.OnDetaching();
        }

        private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(null);
        }

        private void OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
                return;

            Point currentPos = e.GetPosition(null);
            Vector diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
                Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance)
            {
                return;
            }

            LocomotiveModel loco = null;
            if (AssociatedObject is ListBox listBox)
            {
                loco = listBox.SelectedItem as LocomotiveModel;
            }
            else
            {
                loco = AssociatedObject.DataContext as LocomotiveModel;
            }

            if (loco == null || loco.IsForecastGhost)
                return;

            DragDrop.DoDragDrop(AssociatedObject, loco, DragDropEffects.Move);
        }
    }
}
