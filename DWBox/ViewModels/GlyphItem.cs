using System.Windows;

namespace DWBox
{
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
