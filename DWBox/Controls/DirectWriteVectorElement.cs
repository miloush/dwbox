using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;
using Win32.DWrite;

namespace DWBox
{
    public class DirectWriteVectorElement : DirectWriteElement
    {
        public static DependencyProperty FillProperty = Shape.FillProperty.AddOwner(typeof(DirectWriteVectorElement), new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender));
        
        public Brush Fill
        {
            get { return (Brush)GetValue(FillProperty); }
            set { SetValue(FillProperty, value); }
        }

        public RenderDetails Details { get; private set; }

        protected override void OnTextLayoutChanged()
        {
            InvalidateGeometry();

            base.OnTextLayoutChanged();
        }

        ItemsControl _container;
        GlyphItem[] _items;

        public DirectWriteVectorElement()
        {
            var panel = new FrameworkElementFactory(typeof(Canvas));
            panel.SetValue(ClipToBoundsProperty, true);
            panel.SetValue(Panel.BackgroundProperty, Brushes.Transparent);

            _container = new ItemsControl();
            _container.ItemsPanel = new ItemsPanelTemplate(panel);

            Style containerStyle = new(typeof(ContentPresenter));
            containerStyle.Setters.Add(new Setter(Canvas.LeftProperty, new Binding(nameof(GlyphItem.Details) + "." + nameof(RenderGlyphDetails.TransformedX))));
            containerStyle.Setters.Add(new Setter(Canvas.TopProperty, new Binding(nameof(GlyphItem.Details) + "." + nameof(RenderGlyphDetails.TransformedY))));
            _container.ItemContainerStyle = containerStyle;

            AddVisualChild(_container);
        }

        private void InvalidateGeometry()
        {
            if (TextLayout is not TextLayout layout)
                return;

            RenderDetails details = new(FontFace, FontSize);
            DetailsRenderer renderer = new(details, isPixelSnappingDisabled: true);
            Render(renderer);

            _container.ItemsSource = _items = details.Select(d => new GlyphItem(d)).ToArray();
        }

        protected override Visual GetVisualChild(int index) => _container;
        protected override int VisualChildrenCount => 1;

        protected override Size MeasureOverride(Size availableSize)
        {
            _container.Measure(availableSize);
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize); // sets TextLayout
            _container.Arrange(new Rect(default, finalSize));
            return size;
        }

        public override void Highlight(RenderGlyphDetails details)
        {
            var items = _items;
            GlyphItem highlightedGlyph = null;

            foreach (var item in items)
            {
                if (details == null)
                {
                    item.IsRunHighlighted = item.IsClusterHighlighted = item.IsGlyphHighlighted = false;
                    continue;
                }

                item.IsRunHighlighted = item.Details.RunDetails.TextPosition == details.RunDetails.TextPosition &&
                                        item.Details.RunDetails.TextLength == details.RunDetails.TextLength;

                if (item.IsRunHighlighted && item.Details.ClusterStartIndex == details.ClusterStartIndex)
                {
                    item.IsClusterHighlighted = true;
                    item.IsGlyphHighlighted = item.Details.ClusterGlyphCount == details.ClusterGlyphCount && item.Details.ClusterGlyphIndex == details.ClusterGlyphIndex;
                }
                else
                {
                    item.IsClusterHighlighted = false;
                    item.IsGlyphHighlighted = item.Details.ClusterLength == 1 && item.Details.ClusterLength == 1 && item.Details.RunDetails.TextPosition + item.Details.ClusterStartIndex == details.RunDetails.TextPosition + details.ClusterStartIndex;
                }

                if (item.IsGlyphHighlighted)
                    highlightedGlyph = item;
            }

            // TODO: DirectWriteElement shouldn't have dependency on BoxItem, maybe have a GlyphHighlighted event
            if (DataContext is BoxItem boxItem)
                boxItem.HighlightedGlyph = highlightedGlyph;
        }
    }
}
