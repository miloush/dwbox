using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Win32.DWrite;

namespace DWBox
{
    public class RenderDetails : Collection<RenderGlyphDetails>
    {
        private ushort _designUnitsPerEm;
        private float _emSize;
        private string _name;

        public RenderDetails(FontFace fontFace, float emSize)
        {
            _emSize = emSize;
            _designUnitsPerEm = fontFace.Metrics.DesignUnitsPerEm;
            
            _name = fontFace.FullName;

            // imitating BoxItem.NameVersion
            if (fontFace.Version is string version)
                if (version.StartsWith("Version ", System.StringComparison.OrdinalIgnoreCase))
                    _name = string.Join(" ", fontFace.FullName, version.Substring("Version ".Length));
        }

        public string Name => _name;
        public float EmSize => _emSize;
        public ushort DesignUnitsPerEm => _designUnitsPerEm;

        //private bool _noTypeface;
        //private GlyphTypeface _typeface;
        //public GlyphTypeface GlyphTypeface
        //{
        //    get
        //    {
        //        if (_noTypeface) 
        //            return null;

        //        if (_typeface == null && _item.FilePath is string path)
        //        {
        //            try { _typeface = new GlyphTypeface(new System.Uri(path)); }
        //            catch { _noTypeface = true; }
        //        }

        //        return _typeface;
        //    }
        //}
    }
}
