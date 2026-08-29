using System;
using System.Runtime.InteropServices;
using Win32.D2D1;
using Win32.DWrite;

namespace DWBox
{
    public class DetailsRenderer : DWrite.IDWriteTextRenderer1
    {
        public RenderDetails Details { get; }
        private bool _isPixelSnappingDisabled;
        private int _runIndex = 0;

        public DetailsRenderer(RenderDetails details, bool isPixelSnappingDisabled)
        {
            Details = details;
            _isPixelSnappingDisabled = isPixelSnappingDisabled;
        }

        public bool IsPixelSnappingDisabled(IntPtr clientDrawingContext) => _isPixelSnappingDisabled;
        public Matrix GetCurrentTransform(IntPtr clientDrawingContext) => Matrix.Identity;
        public float GetPixelsPerDip(IntPtr clientDrawingContext) => 1f;

        public void DrawGlyphRun(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, IntPtr glyphRun, IntPtr glyphRunDescription, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect)
        {
            DrawGlyphRun(clientDrawingContext, baselineOriginX, baselineOriginY, GlyphOrientationAngle.Degrees0, measuringMode, glyphRun, glyphRunDescription, clientDrawingEffect);
        }

        public void DrawGlyphRun(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, GlyphOrientationAngle orientationAngle, MeasuringMode measuringMode, IntPtr glyphRun, IntPtr glyphRunDescription, object clientDrawingEffect)
        {
            GlyphRun run = Marshal.PtrToStructure<GlyphRun>(glyphRun);
            GlyphRunDescription desc = Marshal.PtrToStructure<GlyphRunDescription>(glyphRunDescription);

            RenderRunDetails runDetails = new()
            {
                BaselineOriginX = baselineOriginX,
                BaselineOriginY = baselineOriginY,
                Index = _runIndex,
                LocaleName = desc.LocaleName,
                Text = desc.Text,
                TextPosition = desc.TextPosition,
                TextLength = desc.TextLength,
                FontName = run.FontFace.FullName,
                BidiLevel = run.BidiLevel,
                OrientationAngle = orientationAngle switch
                {
                    GlyphOrientationAngle.Degrees90 => 90,
                    GlyphOrientationAngle.Degrees180 => 180,
                    GlyphOrientationAngle.Degrees270 => 270,
                    _ => 0,
                }
            };

            float[] advances = run.GetGlyphAdvances();
            ushort[] glyphIndices = run.GetGlyphIndices();
            GlyphOffset[] glyphOffsets = run.GetGlyphOffsets();

            Matrix transform = Matrix.Create(orientationAngle, run.IsSideways, baselineOriginX, baselineOriginY);
            Point2F[] glyphOrigins = DWriteFactory.Shared.ComputeGlyphOrigins(glyphRun, run.GlyphCount, measuringMode, baselineOriginX, baselineOriginY, Matrix.Identity);

            RenderGlyphDetails[] glyphs = new RenderGlyphDetails[run.GlyphCount];
            System.Windows.Media.RotateTransform wpfTrasform = new(runDetails.OrientationAngle, 0, 0);
            wpfTrasform.Freeze();

            for (int i = 0; i < run.GlyphCount; i++)
            {
                float advanceOffset = glyphOffsets[i].AdvanceOffset;
                float ascenderOffset = glyphOffsets[i].AscenderOffset;

                Point2F origin = transform.Transform(glyphOrigins[i]);

                glyphs[i] = new RenderGlyphDetails(Details)
                {
                    RunDetails = runDetails,
                    RunGlyphIndex = i,
                    GlyphID = glyphIndices[i],
                    Advance = advances[i],
                    AdvanceOffset = advanceOffset,
                    AscenderOffset = ascenderOffset,
                    OriginX = glyphOrigins[i].X - baselineOriginX,
                    OriginY = glyphOrigins[i].Y - baselineOriginY,
                    TransformedX = origin.X,
                    TransformedY = origin.Y,
                };

                // since we are using ComputeGlyphOrigins, all layout is taken into account
                // passing it to GetGlyphRunOutline would apply it second time
                // we want the glyph geometry to start at 0,0
                StreamGeometrySink sink = new();
                run.FontFace.GetGlyphRunOutline(run.FontEmSize, new ushort[] { glyphIndices[i] }, null, null, false, false, sink);
                sink.Geometry.Transform = wpfTrasform;                
                glyphs[i].GlyphGeometry = sink.Geometry;
            }

            short[] clusterMap = desc.GetClusterMap();
            for (int i = 0; i < clusterMap.Length; i++)
            {
                int index = clusterMap[i];
                int codepoint = desc.Text[i];
                glyphs[index].Codepoints.Add(codepoint);
                glyphs[index].ClusterStartIndex = i;
            }

            int clusterIndex = -1;
            int lastClusterStart = 0;
            int clusterGlyphIndex = 0;
            if (Details.Count > 0)
            {
                // continue from last recorded run details
                var lastDetail = Details[Details.Count - 1];
                clusterIndex = lastDetail.ClusterIndex;
                lastClusterStart = lastDetail.ClusterStartIndex;
                clusterGlyphIndex = lastDetail.ClusterGlyphIndex + 1;
            }

            int firstClusterGlyph = -1;
            for (int i = 0; i < glyphs.Length; i++)
            {
                RenderGlyphDetails glyph = glyphs[i];
                if (glyph.Codepoints.Count > 0)
                {
                    if (firstClusterGlyph >= 0)
                        for (int j = firstClusterGlyph; j < i; j++)
                            glyphs[j].ClusterGlyphCount = i - firstClusterGlyph;

                    clusterIndex++;
                    clusterGlyphIndex = 0;
                    lastClusterStart = glyph.ClusterStartIndex;
                    firstClusterGlyph = i;
                }

                glyph.RunIndex = _runIndex;
                glyph.Index = Details.Count;
                glyph.ClusterIndex = clusterIndex;
                glyph.ClusterStartIndex = lastClusterStart;
                glyph.ClusterGlyphIndex = clusterGlyphIndex++;
                Details.Add(glyph);
            }

            if (firstClusterGlyph > 0)
                for (int j = firstClusterGlyph; j < glyphs.Length; j++)
                    glyphs[j].ClusterGlyphCount = glyphs.Length - firstClusterGlyph;

            _runIndex++;
        }

        public void DrawUnderline(IntPtr clientDrwaingContext, float baselineOriginX, float baselineOriginY, IntPtr underline, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawUnderline(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, GlyphOrientationAngle orientationAngle, IntPtr underline, object clientDrawingEffect) { }
        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr strikethrough, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, GlyphOrientationAngle orientationAngle, IntPtr strikethrough, object clientDrawingEffect) { }
        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, IntPtr inlineObject, bool isSideways, bool isRightToLeft, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, GlyphOrientationAngle orientationAngle, IntPtr inlineObject, bool isSideways, bool isRightToLeft, object clientDrawingEffect) { }
    }
}
