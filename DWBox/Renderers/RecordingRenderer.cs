using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Win32.DWrite;

namespace DWBox
{
    public class RecordingRenderer : DWrite.IDWriteTextRenderer
    {
        public GlyphRunDetails Details { get; }

        public RecordingRenderer(GlyphRunDetails details)
        {
            Details = details;
        }

        public bool IsPixelSnappingDisabled(IntPtr clientDrawingContext) => false;
        public Matrix GetCurrentTransform(IntPtr clientDrawingContext) => new Matrix { M11 = 1, M22 = 1 };
        public float GetPixelsPerDip(IntPtr clientDrawingContext) => 96f;

        public void DrawGlyphRun(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, IntPtr glyphRun, IntPtr glyphRunDescription, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect)
        {
            GlyphRun run = Marshal.PtrToStructure<GlyphRun>(glyphRun);
            GlyphRunDescription desc = Marshal.PtrToStructure<GlyphRunDescription>(glyphRunDescription);

            float[] advances = run.GetGlyphAdvances();
            ushort[] glyphIndices = run.GetGlyphIndices();
            GlyphOffset[] glyphOffsets = run.GetGlyphOffsets();

            GlyphRunDetailsItem[] items = new GlyphRunDetailsItem[run.GlyphCount];

            for (int i = 0; i < run.GlyphCount; i++)
                items[i] = new GlyphRunDetailsItem(Details)
                {
                    GlyphID = glyphIndices[i],
                    Advance = advances[i],
                    AdvanceOffset = glyphOffsets[i].AdvanceOffset,
                    AscenderOffset = glyphOffsets[i].AscenderOffset,
                    FontName = run.FontFace.FullName,
                };

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

                item.Index = Details.Count;
                item.ClusterIndex = clusterIndex;
                Details.Add(item);
            }
        }

        public void DrawUnderline(IntPtr clientDrwaingContext, float baselineOriginX, float baselineOriginY, IntPtr underline, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr strikethrough, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, IntPtr inlineObject, bool isSideways, bool isRightToLeft, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
    }
}
