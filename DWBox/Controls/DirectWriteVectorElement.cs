using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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

        ItemsControl _items;

        public DirectWriteVectorElement()
        {
            _items = new ItemsControl();
            _items.ItemsPanel = new ItemsPanelTemplate(new FrameworkElementFactory(typeof(Grid)));

            AddVisualChild(_items);
        }

        private void InvalidateGeometry()
        {
            if (TextLayout is not TextLayout layout)
                return;

            RenderDetails details = new(FontFace, FontSize);
            DetailsRenderer renderer = new(details, isPixelSnappingDisabled: true);
            Render(renderer);

            _items.ItemsSource = details.Select(d => new GlyphItem(d));
        }

        protected override Visual GetVisualChild(int index) => _items;
        protected override int VisualChildrenCount => 1;

        protected override Size MeasureOverride(Size availableSize)
        {
            _items.Measure(availableSize);
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize); // sets TextLayout
            _items.Arrange(new Rect(default, finalSize));
            return size;
        }
    }

    public class GlyphItem : DependencyObject
    {
        private RenderGlyphDetails _details;
        public RenderGlyphDetails Details => _details;

        public GlyphItem(RenderGlyphDetails details)
        {
            _details = details;
        }

        public Thickness OriginMargin => new Thickness(_details.TransformedX, _details.TransformedY, 0, 0);
    }
}
