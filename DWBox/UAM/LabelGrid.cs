//
// © 2019-2020 miloush.net. All rights reserved.
//

using System;
using System.Windows;
using System.Windows.Controls;

namespace UAM.InformatiX.Windows.Controls
{
    public class LabelGrid : Panel
    {
        // TOOD: alignments - maybe styles, scrolling, virtualization, alternating row background

        public static readonly DependencyProperty VerticalSpacingProperty = DependencyProperty.Register(nameof(VerticalSpacing), typeof(double), typeof(LabelGrid), new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty HorizontalSpacingProperty = DependencyProperty.Register(nameof(HorizontalSpacing), typeof(double), typeof(LabelGrid), new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsMeasure));
        public static readonly DependencyProperty LabelStyleProperty = DependencyProperty.Register(nameof(LabelStyle), typeof(Style), typeof(LabelGrid));
        public static readonly DependencyProperty ContentStyleProperty = DependencyProperty.Register(nameof(ContentStyle), typeof(Style), typeof(LabelGrid));

        public double VerticalSpacing
        {
            get { return (double)GetValue(VerticalSpacingProperty); }
            set { SetValue(VerticalSpacingProperty, value); }
        }

        public double HorizontalSpacing
        {
            get { return (double)GetValue(HorizontalSpacingProperty); }
            set { SetValue(HorizontalSpacingProperty, value); }
        }

        public Style ContentStyle
        {
            get { return (Style)GetValue(ContentStyleProperty); }
            set { SetValue(ContentStyleProperty, value); }
        }

        public Style LabelStyle
        {
            get { return (Style)GetValue(LabelStyleProperty); }
            set { SetValue(LabelStyleProperty, value); }
        }


        private double _maxLabelWidth;
        protected override Size MeasureOverride(Size availableSize)
        {
            availableSize.Height = double.PositiveInfinity;
            UIElementCollection children = InternalChildren;

            // When the available size is smaller than labels + content would desire, we will give all available space to labels (arbitrary choice).
            // This needs to be done in two passes: finding the widest label and then measuring the contents with the remaining space.
            _maxLabelWidth = 0;

            for (int i = 0; i < children.Count; i += 2)
            {
                UIElement label = children[i];
                if (label == null) continue;

                label.Measure(availableSize);
                _maxLabelWidth = Math.Max(_maxLabelWidth, label.DesiredSize.Width);
            }

            double maxContentWidth = 0;
            availableSize.Width -= _maxLabelWidth + HorizontalSpacing;

            // The height must be tracked simultaneously for label and content as they need to be aligned.
            double desiredHeight = 0;
            int lines = 0;

            for (int i = 1; i < children.Count; i += 2)
            {
                UIElement content = children[i];
                double contentHeight = 0;
                if (content != null)
                {
                    content.Measure(availableSize);
                    maxContentWidth = Math.Max(maxContentWidth, content.DesiredSize.Width);
                    contentHeight = content.DesiredSize.Height;
                }

                UIElement label = children[i - 1];
                double labelHeight = 0;
                if (label != null)
                    labelHeight = label.DesiredSize.Height;

                lines++;
                desiredHeight += Math.Max(labelHeight, contentHeight);
            }

            if (lines > 0)
                desiredHeight += VerticalSpacing * (lines - 1);

            return new Size(_maxLabelWidth + HorizontalSpacing + maxContentWidth, desiredHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            UIElementCollection children = InternalChildren;
            bool even = children.Count % 2 == 0;

            double labelLeft = 0;
            double contentLeft = _maxLabelWidth + HorizontalSpacing;
            double contentWidth = finalSize.Width - contentLeft;

            double top = 0;

            for (int i = 0; i < children.Count; i += 2)
            {
                double rowHeight = 0;

                UIElement label = children[i];
                if (label?.DesiredSize.Height > rowHeight)
                    rowHeight = label.DesiredSize.Height;

                UIElement content = null;
                if (even || (i + 1 < children.Count)) content = children[i + 1];
                if (content?.DesiredSize.Height > rowHeight)
                    rowHeight = content.DesiredSize.Height;

                Rect labelRect = new Rect(labelLeft, top, _maxLabelWidth, rowHeight);
                label?.Arrange(labelRect);

                Rect contentRect = new Rect(contentLeft, top, contentWidth, rowHeight);
                content?.Arrange(contentRect);

                top += rowHeight + VerticalSpacing;
            }

            return new Size(finalSize.Width, top - VerticalSpacing);
        }

        protected override void OnVisualChildrenChanged(DependencyObject visualAdded, DependencyObject visualRemoved)
        {
            if (visualAdded is FrameworkElement elementAdded)
            {
                int index = InternalChildren.IndexOf(elementAdded);
                bool isLabel = index % 2 == 0;

                if (isLabel)
                {
                    if (elementAdded.Style == null && LabelStyle != null)
                        elementAdded.Style = LabelStyle;

                    if (visualAdded is Label label && label.Target == null)
                        if (index >= 0 && index < InternalChildren.Count - 1)
                            label.Target = InternalChildren[index + 1];
                }
                else
                {
                    if (elementAdded.Style == null && ContentStyle != null)
                        elementAdded.Style = ContentStyle;

                    if (index > 0 && index < InternalChildren.Count)
                        if (InternalChildren[index - 1] is Label label && label.Target == null)
                            label.Target = elementAdded;
                }
            }
        }
    }
}
