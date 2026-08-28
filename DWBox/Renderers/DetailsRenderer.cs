using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Win32.DWrite;

namespace DWBox
{
    public class DetailsRenderer : DWrite.IDWriteTextRenderer
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
        public float GetPixelsPerDip(IntPtr clientDrawingContext) => 96f;

        public void DrawGlyphRun(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, IntPtr glyphRun, IntPtr glyphRunDescription, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect)
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
                FontName = run.FontFace.FullName
            };

            float x = baselineOriginX;
            float y = baselineOriginY;

            float[] advances = run.GetGlyphAdvances();
            ushort[] glyphIndices = run.GetGlyphIndices();
            GlyphOffset[] glyphOffsets = run.GetGlyphOffsets();

            RenderGlyphDetails[] items = new RenderGlyphDetails[run.GlyphCount];

            for (int i = 0; i < run.GlyphCount; i++)
            {
                float advanceX = glyphOffsets[i].AdvanceOffset;
                float advanceY = glyphOffsets[i].AscenderOffset;

                items[i] = new RenderGlyphDetails(Details)
                {
                    RunDetails = runDetails,
                    GlyphID = glyphIndices[i],
                    Advance = advances[i],
                    AdvanceOffset = advanceX,
                    AscenderOffset = advanceY,
                    X = x,
                    Y = y,
                };

                x += advanceX;
                y += advanceY;

                StreamGeometrySink sink = new();
                run.FontFace.GetGlyphRunOutline(run, i, sink);
                items[i].GlyphGeometry = sink.Geometry;
            }

            short[] clusterMap = desc.GetClusterMap();
            for (int i = 0; i < clusterMap.Length; i++)
            {
                int index = clusterMap[i];
                int codepoint = desc.Text[i];
                items[index].Codepoints.Add(codepoint);
            }

            int clusterIndex = -1;
            if (Details.Count > 0)
                clusterIndex = Details[Details.Count - 1].ClusterIndex;
            
            foreach (var item in items)
            {
                if (item.Codepoints.Count > 0)
                    clusterIndex++;

                item.RunIndex = _runIndex;
                item.Index = Details.Count;
                item.ClusterIndex = clusterIndex;
                Details.Add(item);
            }

            _runIndex++;
        }

        public void DrawUnderline(IntPtr clientDrwaingContext, float baselineOriginX, float baselineOriginY, IntPtr underline, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr strikethrough, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, IntPtr inlineObject, bool isSideways, bool isRightToLeft, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
    }
}
