using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Win32.DWrite;

namespace DWBox
{
    public class BitmapRenderer : DWrite.IDWriteTextRenderer
    {
        private readonly DWrite.IDWriteBitmapRenderTarget _bitmapRenderTarget;
        private readonly DWrite.IDWriteRenderingParams _renderingParams;

        internal BitmapRenderer(DWrite.IDWriteBitmapRenderTarget bitmapRenderTarget, DWrite.IDWriteRenderingParams renderingParams)
        {
            _bitmapRenderTarget = bitmapRenderTarget;
            _renderingParams = renderingParams;
        }

        internal uint TextColor { get; set; }
        public bool IsPixelSnappingDisabled(IntPtr clientDrawingContext) => false;
        public Matrix GetCurrentTransform(IntPtr clientDrawingContext) => Matrix.Identity;
        public float GetPixelsPerDip(IntPtr clientDrawingContext) => 96f;

        public void DrawGlyphRun(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, IntPtr glyphRun, IntPtr glyphRunDescription, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect)
        {
            _bitmapRenderTarget.DrawGlyphRun(baselineOriginX, baselineOriginY, measuringMode, glyphRun, _renderingParams, TextColor, IntPtr.Zero);
        }

        public void DrawUnderline(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr underline, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr strikethrough, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, IntPtr inlineObject, bool isSideways, bool isRightToLeft, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
    }
}
