using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace DWBox
{
    public class RenderGlyphDetails
    {
        private RenderDetails _details;

        public RenderGlyphDetails(RenderDetails details)
        {
            _details = details;
        }

        public RenderRunDetails RunDetails { get; set; }

        public int Index { get; set; }
        public int RunIndex { get; set; }
        public int ClusterIndex { get; set; }
        public ushort GlyphID { get; set; }
        public float Advance { get; set; }
        public float AdvanceOffset { get; set; }
        public float AscenderOffset { get; set; }

        public float OriginX { get; set; }
        public float OriginY { get; set; }
        public float TransformedX { get; set; }
        public float TransformedY { get; set; }

        private int ToDesign(float x) => (int)(x / _details.EmSize * _details.DesignUnitsPerEm);
        public int DesignAdvance => ToDesign(Advance);
        public int DesignAdvanceOffset => ToDesign(AdvanceOffset);
        public int DesignAscenderOffset => ToDesign(AscenderOffset);
        public int DesignOriginX => ToDesign(OriginX);
        public int DesignOriginY => ToDesign(OriginY);

        public int ClusterStartIndex { get; set; }
        public int ClusterLength => Codepoints.Count;
        public int ClusterGlyphIndex { get; set; } 
        public int ClusterGlyphCount { get; set; }

        public int RunGlyphIndex { get; set; }

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
                Geometry geometry = GlyphGeometry; // ?? _details?.GlyphTypeface?.GetGlyphOutline(GlyphID, _details.EmSize, _details.EmSize);
                if (geometry == null)
                    return null;

                return new DrawingImage(new GeometryDrawing(Brushes.Black, null, geometry));
            }
        }

        public PathGeometry GlyphGeometry { get; set; }

    }
}
