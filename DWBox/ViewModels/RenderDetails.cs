using System.Collections.ObjectModel;
using System.Windows.Media;

namespace DWBox
{
    public class RenderDetails : Collection<RenderGlyphDetails>
    {
        private BoxItem _item;
        private ushort _designUnitsPerEm;

        public RenderDetails(BoxItem item, ushort designUnitsPerEm)
        {
            _item = item;
            _designUnitsPerEm = designUnitsPerEm;
        }

        public string Name => _item.NameVersion;
        public float EmSize => _item.RenderingElement?.FontSize ?? 48f;
        public ushort DesignUnitsPerEm => _designUnitsPerEm;

        private bool _noTypeface;
        private GlyphTypeface _typeface;
        public GlyphTypeface GlyphTypeface
        {
            get
            {
                if (_noTypeface) 
                    return null;

                if (_typeface == null && _item.FilePath is string path)
                {
                    try { _typeface = new GlyphTypeface(new System.Uri(path)); }
                    catch { _noTypeface = true; }
                }

                return _typeface;
            }
        }
    }
}
