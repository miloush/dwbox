using System.Windows;
using System.Windows.Controls;

namespace DWBox
{
    public class ProportionalPanel : Panel
    {
        private static readonly DependencyProperty OrientationProperty = StackPanel.OrientationProperty.AddOwner(typeof(ProportionalPanel), new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        private double _totalDesiredWidth;
        private double _totalDesiredHeight;

        protected override Size MeasureOverride(Size availableSize)
        {
            if (InternalChildren.Count < 1)
            {
                return base.MeasureOverride(availableSize);
            }
            else if (InternalChildren.Count == 1)
            {
                InternalChildren[0].Measure(availableSize);
                return InternalChildren[0].DesiredSize;
            }

            if (Orientation == Orientation.Horizontal)
            {
                _totalDesiredWidth = 0;
                double maxDesiredHeight = 0;
                Size unrestricted = new Size(double.PositiveInfinity, availableSize.Height);

                foreach (UIElement child in InternalChildren)
                {
                    child.Measure(unrestricted);
                    _totalDesiredWidth += child.DesiredSize.Width;
                    if (child.DesiredSize.Height > maxDesiredHeight)
                        maxDesiredHeight = child.DesiredSize.Height;
                }

                if (_totalDesiredWidth <= availableSize.Width && maxDesiredHeight <= availableSize.Height)
                    return new Size(_totalDesiredWidth, maxDesiredHeight);

                maxDesiredHeight = 0;
                double totalDesiredWidth = 0;
                foreach (UIElement child in InternalChildren)
                {
                    Size size = new(child.DesiredSize.Width / _totalDesiredWidth * availableSize.Width, availableSize.Height);
                    child.Measure(size);
                    totalDesiredWidth += child.DesiredSize.Width;
                    if (child.DesiredSize.Height > maxDesiredHeight)
                        maxDesiredHeight = child.DesiredSize.Height;
                }

                _totalDesiredWidth = totalDesiredWidth;
                return new Size(_totalDesiredWidth, maxDesiredHeight);
            }
            else
            {
                _totalDesiredHeight = 0;
                double maxDesiredWidth = 0;
                Size unrestricted = new Size(availableSize.Width, double.PositiveInfinity);

                foreach (UIElement child in InternalChildren)
                {
                    child.Measure(unrestricted);
                    _totalDesiredHeight += child.DesiredSize.Height;
                    if (child.DesiredSize.Width > maxDesiredWidth)
                        maxDesiredWidth = child.DesiredSize.Width;
                }

                if (_totalDesiredHeight <= availableSize.Height && maxDesiredWidth <= availableSize.Width)
                    return new Size(maxDesiredWidth, _totalDesiredHeight);

                maxDesiredWidth = 0;
                double totalDesiredHeight = 0;
                foreach (UIElement child in InternalChildren)
                {
                    Size size = new(availableSize.Width, child.DesiredSize.Height / _totalDesiredHeight * availableSize.Height);
                    child.Measure(size);
                    totalDesiredHeight += child.DesiredSize.Height;
                    if (child.DesiredSize.Width > maxDesiredWidth)
                        maxDesiredWidth = child.DesiredSize.Width;
                }

                _totalDesiredHeight = totalDesiredHeight;
                return new Size(maxDesiredWidth, _totalDesiredHeight);
            }
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Children.Count < 1)
            {
                return base.ArrangeOverride(finalSize);
            }
            else if (InternalChildren.Count == 1)
            {
                InternalChildren[0].Arrange(new Rect(default, finalSize));
                return finalSize;
            }

            double spacing = 0;
            double adjustment = 0;
            if (Orientation == Orientation.Horizontal)
            {
                if (finalSize.Width > _totalDesiredWidth)
                    spacing = (finalSize.Width - _totalDesiredWidth) / (InternalChildren.Count - 1);
                else
                    adjustment = (finalSize.Width - _totalDesiredWidth) / InternalChildren.Count;

                double x = 0;
                foreach (UIElement child in InternalChildren)
                {
                    double finalWidth = child.DesiredSize.Width + adjustment;
                    Rect rect = new Rect(x, 0, finalWidth, finalSize.Height);
                    child.Arrange(rect);
                    x += finalWidth + spacing;
                }
            }
            else
            {
                if (finalSize.Height > _totalDesiredHeight)
                    spacing = (finalSize.Height - _totalDesiredHeight) / (InternalChildren.Count - 1);
                else
                    adjustment = (finalSize.Height - _totalDesiredHeight) / InternalChildren.Count;

                double y = 0;
                foreach (UIElement child in InternalChildren)
                {
                    double finalHeight = child.DesiredSize.Height + adjustment;
                    Rect rect = new Rect(0, y, finalSize.Width, finalHeight);
                    child.Arrange(rect);
                    y += finalHeight + spacing;
                }
            }

            return finalSize;
        }
    }
}