using System.Windows;
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

        private PathGeometry _geometry;
        private Path _path;

        protected override void OnTextLayoutChanged()
        {
            base.OnTextLayoutChanged();

            InvalidateGeometry();
        }

        private void InvalidateGeometry()
        {
            if (TextLayout is not TextLayout layout)
                return;

            VectorRenderer renderer = new();
            Render(renderer);
            _geometry = renderer.Geometry;

            if (_path != null)
                RemoveVisualChild(_path);

            _path = new Path
            {
                Data = _geometry,
                Fill = Fill
            };

            AddVisualChild(_path);
        }

        protected override Visual GetVisualChild(int index)
        {
            return _path;
        }

        protected override int VisualChildrenCount => _path == null ? 0 : 1;

        protected override Size MeasureOverride(Size availableSize)
        {
            Size size = base.MeasureOverride(availableSize);
            _path?.Measure(availableSize);
            return size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize); // sets TextLayout
            _path?.Arrange(new Rect(default, finalSize));
            return size;
        }
    }
}
