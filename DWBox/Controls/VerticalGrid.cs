using System;
using System.Windows;
using System.Windows.Controls;

namespace DWBox
{
    // basically uniform grid, but adding columns only on full rows
    // prefers vertical arrangement, not really uniform
    public class VerticalGrid : Panel
    {
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

            return new Size((maxChildDesiredWidth * _columns), (maxChildDesiredHeight * _rows));
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            Rect childBounds = new Rect(0, 0, arrangeSize.Width / _columns, arrangeSize.Height / _rows);
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

            _columns = (int)Math.Sqrt(nonCollapsedCount);
            _rows = nonCollapsedCount / _columns;
            if ((_rows * _columns) < nonCollapsedCount)
                _rows++;
        }

        private int _rows;
        private int _columns;
    }
}