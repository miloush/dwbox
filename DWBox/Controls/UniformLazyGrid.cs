using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.TextFormatting;

namespace DWBox
{
    // Basically uniform grid, but only adds column/row when it has enough elements to fill it

    // UniformGrid:     *   * *   * *   * * 
    //                      _ _   * _   * * 
    //
    //
    // UniformLazyGrid: *    *     *    * * 
    //                       *     *    * * 
    //                             *        

    public class UniformLazyGrid : Panel
    {
        private static readonly DependencyProperty OrientationProperty = StackPanel.OrientationProperty.AddOwner(typeof(UniformLazyGrid), new FrameworkPropertyMetadata(Orientation.Vertical, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        protected override Size MeasureOverride(Size constraint)
        {
            UpdateComputedValues();

            Size childConstraint = new Size(constraint.Width / _columns, constraint.Height / _rows);
            double maxChildDesiredWidth = 0.0;
            double maxChildDesiredHeight = 0.0;

            for (int i = 0, count = InternalChildren.Count; i < count; ++i)
            {
                UIElement child = InternalChildren[i];
                child.Measure(childConstraint);
                Size childDesiredSize = child.DesiredSize;

                if (maxChildDesiredWidth < childDesiredSize.Width)
                    maxChildDesiredWidth = childDesiredSize.Width;

                if (maxChildDesiredHeight < childDesiredSize.Height)
                    maxChildDesiredHeight = childDesiredSize.Height;
            }

            return new Size(maxChildDesiredWidth * _columns, maxChildDesiredHeight * _rows);
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            Rect childBounds = new Rect(0, 0, arrangeSize.Width / _columns, arrangeSize.Height / _rows);

            if (Orientation == Orientation.Vertical)
            {
                double xStep = childBounds.Width;
                double xBound = arrangeSize.Width - 1.0;

                foreach (UIElement child in InternalChildren)
                {
                    child.Arrange(childBounds);

                    if (child.Visibility != Visibility.Collapsed)
                    {
                        childBounds.X += xStep;
                        if (childBounds.X >= xBound)
                        {
                            childBounds.Y += childBounds.Height;
                            childBounds.X = 0;
                        }
                    }
                }
            }
            else
            {
                double yStep = childBounds.Height;
                double yBound = arrangeSize.Height - 1.0;

                foreach (UIElement child in InternalChildren)
                {
                    child.Arrange(childBounds);

                    if (child.Visibility != Visibility.Collapsed)
                    {
                        childBounds.Y += yStep;
                        if (childBounds.Y >= yBound)
                        {
                            childBounds.X += childBounds.Width;
                            childBounds.Y = 0;
                        }
                    }
                }
            }

            return arrangeSize;
        }

        private void UpdateComputedValues()
        {
            int nonCollapsedCount = 0;

            for (int i = 0, count = InternalChildren.Count; i < count; ++i)
            {
                UIElement child = InternalChildren[i];
                if (child.Visibility != Visibility.Collapsed)
                    nonCollapsedCount++;
            }

            if (nonCollapsedCount == 0)
                nonCollapsedCount = 1;

            if (Orientation == Orientation.Vertical)
            {
                _columns = (int)Math.Sqrt(nonCollapsedCount);
                _rows = nonCollapsedCount / _columns;
                if ((_rows * _columns) < nonCollapsedCount)
                    _rows++;
            }
            else
            {
                _rows = (int)Math.Sqrt(nonCollapsedCount);
                _columns = nonCollapsedCount / _rows;
                if ((_rows * _columns) < nonCollapsedCount)
                    _columns++;
            }
        }

        private int _rows;
        private int _columns;
    }
}