using System.Collections.Generic;
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

        private List<Path> _paths = new();

        protected override void OnTextLayoutChanged()
        {
            InvalidateGeometry();

            base.OnTextLayoutChanged();
        }

        private void InvalidateGeometry()
        {
            if (TextLayout is not TextLayout layout)
                return;

            VectorRenderer renderer = new();
            Render(renderer);

            for (int i = _paths.Count - 1; i >= 0; i--)
            {
                Path path = _paths[i];
                _paths.RemoveAt(i); // Remove queries current count
                RemoveVisualChild(path);
            }

            foreach (var geometry in renderer.RunGeometries)
            {
                Path path = new()
                {
                    Data = geometry,
                    Fill = Fill
                };

                _paths.Add(path);
                AddVisualChild(path);
            }
        }

        protected override Visual GetVisualChild(int index)
        {
            return _paths[index];
        }

        protected override int VisualChildrenCount => _paths.Count;

        protected override Size MeasureOverride(Size availableSize)
        {
            foreach (var path in _paths)
                path.Measure(availableSize);
            
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            Size size = base.ArrangeOverride(finalSize); // sets TextLayout
            
            foreach (var path in _paths)
                path.Arrange(new Rect(default, finalSize));

            return size;
        }
    }
}
