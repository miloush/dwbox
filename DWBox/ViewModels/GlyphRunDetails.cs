using System.Collections.ObjectModel;
using System.Windows.Media;

namespace DWBox
{
    public class GlyphRunDetails : Collection<GlyphRunDetailsItem>
    {
        private BoxItem _item;

        public GlyphRunDetails(BoxItem item)
        {
            _item = item;
        }

        public string Name => _item.NameVersion;
        public float EmSize => _item.RenderingElement?.FontSize ?? 48f;

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
