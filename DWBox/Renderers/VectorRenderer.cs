using System;
using System.Runtime.InteropServices;
using Win32.DWrite;

namespace DWBox
{
    public class VectorRenderer : DWrite.IDWriteTextRenderer
    {
        public System.Windows.Media.PathGeometry Geometry => _sink.Geometry;

        private StreamGeometrySink _sink;

        public VectorRenderer()
        {
            _sink = new StreamGeometrySink();
        }

        public bool IsPixelSnappingDisabled(IntPtr clientDrawingContext) => true;
        public Matrix GetCurrentTransform(IntPtr clientDrawingContext) => Matrix.Identity;
        public float GetPixelsPerDip(IntPtr clientDrawingContext) => 96f;

        public void DrawGlyphRun(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, IntPtr pGlyphRun, IntPtr glyphRunDescription, object clientDrawingEffect)
        {
            GlyphRun glyphRun = Marshal.PtrToStructure<GlyphRun>(pGlyphRun);
            glyphRun.FontFace.GetGlyphRunOutline(glyphRun, _sink);
            _sink.Geometry.Transform = new System.Windows.Media.TranslateTransform(baselineOriginX, baselineOriginY);
        }

        public void DrawUnderline(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr underline, object clientDrawingEffect) { }
        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr strikethrough, object clientDrawingEffect) { }
        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, IntPtr inlineObject, bool isSideways, bool isRightToLeft, object clientDrawingEffect) { }
    }
}
