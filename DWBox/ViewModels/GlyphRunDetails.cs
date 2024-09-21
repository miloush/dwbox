using System.Collections.ObjectModel;
using System.Windows.Media;

namespace DWBox
{
    public class GlyphRunDetails : Collection<GlyphRunDetailsItem>
    {
        private BoxItem _item;
        private ushort _upm;

        public GlyphRunDetails(BoxItem item, ushort designUnitsPerEm)
        {
            _item = item;
            _upm = designUnitsPerEm;
        }

        public string Name => _item.NameVersion;
        public float EmSize => _item.RenderingElement?.FontSize ?? 48f;
        public ushort DesignUnitsPerEm => _upm;

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
