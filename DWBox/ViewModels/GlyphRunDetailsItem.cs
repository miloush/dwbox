using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace DWBox
{
    public class GlyphRunDetailsItem
    {
        private GlyphRunDetails _details;

        public GlyphRunDetailsItem(GlyphRunDetails details)
        {
            _details = details;
        }

        public int Index { get; set; }
        public int ClusterIndex { get; set; }
        public ushort GlyphID { get; set; }
        public float Advance { get; set; }
        public float AdvanceOffset { get; set; }
        public float AscenderOffset { get; set; }

        public int DesignAdvance => (int)(Advance / _details.EmSize * _details.DesignUnitsPerEm);
        public int DesignAdvanceOffset => (int)(AdvanceOffset / _details.EmSize * _details.DesignUnitsPerEm);
        public int DesignAscenderOffset => (int)(AscenderOffset / _details.EmSize * _details.DesignUnitsPerEm);

        public string FontName { get; set; }

        public List<int> Codepoints { get; } = new List<int>();
        public string CodepointsString => string.Join(" ", Codepoints.Select(c => c.ToString("X4")));
        public string String
        {
            get
            {
                StringBuilder s = new StringBuilder(Codepoints.Count);
                foreach (int cp in Codepoints)
                    s.Append(char.ConvertFromUtf32(cp));
                return s.ToString();
            }
        }

        public ImageSource GlyphImage
        {
            get
            {
                Geometry geometry = _details?.GlyphTypeface?.GetGlyphOutline(GlyphID, _details.EmSize, _details.EmSize);
                if (geometry == null)
                    return null;

                return new DrawingImage(new GeometryDrawing(Brushes.Black, null, geometry));
            }
        }
    }
}
