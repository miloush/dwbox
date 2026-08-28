using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Win32.DWrite;

namespace DWBox
{
    public class VectorRenderer : DWrite.IDWriteTextRenderer
    {
        public List<System.Windows.Media.PathGeometry> RunGeometries { get; private set; } = new();

        public bool IsPixelSnappingDisabled(IntPtr clientDrawingContext) => true;
        public Matrix GetCurrentTransform(IntPtr clientDrawingContext) => Matrix.Identity;
        public float GetPixelsPerDip(IntPtr clientDrawingContext) => 96f;

        public void DrawGlyphRun(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, IntPtr pGlyphRun, IntPtr glyphRunDescription, object clientDrawingEffect)
        {
            GlyphRun glyphRun = Marshal.PtrToStructure<GlyphRun>(pGlyphRun);
            
            StreamGeometrySink sink = new();
            glyphRun.FontFace.GetGlyphRunOutline(glyphRun, sink);
            sink.Geometry.Transform = new System.Windows.Media.TranslateTransform(baselineOriginX, baselineOriginY);
            RunGeometries.Add(sink.Geometry);
        }

        public void DrawUnderline(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr underline, object clientDrawingEffect) { }
        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr strikethrough, object clientDrawingEffect) { }
        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, IntPtr inlineObject, bool isSideways, bool isRightToLeft, object clientDrawingEffect) { }
    }
}
