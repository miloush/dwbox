using System;
using System.Runtime.InteropServices;
using Win32.DWrite;

namespace DWBox
{
    public class BitmapRenderer : DWrite.IDWriteTextRenderer1
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
        public float GetPixelsPerDip(IntPtr clientDrawingContext) => 1f;

        public void DrawGlyphRun(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, MeasuringMode measuringMode, IntPtr glyphRun, IntPtr glyphRunDescription, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect)
        {
            DrawGlyphRun(clientDrawingContext, baselineOriginX, baselineOriginY, GlyphOrientationAngle.Degrees0, measuringMode, glyphRun, glyphRunDescription, clientDrawingEffect);
        }

        public void DrawUnderline(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr underline, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, IntPtr strikethrough, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }
        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, IntPtr inlineObject, bool isSideways, bool isRightToLeft, [In, MarshalAs(UnmanagedType.IUnknown)] object clientDrawingEffect) { }

        public void DrawGlyphRun(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, GlyphOrientationAngle orientationAngle, MeasuringMode measuringMode, IntPtr glyphRun, IntPtr glyphRunDescription, object clientDrawingEffect)
        {
            GlyphRun run = Marshal.PtrToStructure<GlyphRun>(glyphRun);

            Matrix oldTransform = _bitmapRenderTarget.GetCurrentTransform();
            Matrix runTransform = Matrix.Create(orientationAngle, run.IsSideways, baselineOriginX, baselineOriginY);
            Matrix newTransform = oldTransform * runTransform;

            _bitmapRenderTarget.SetCurrentTransform(newTransform);
            _bitmapRenderTarget.DrawGlyphRun(baselineOriginX, baselineOriginY, measuringMode, glyphRun, _renderingParams, TextColor, IntPtr.Zero);
            _bitmapRenderTarget.SetCurrentTransform(oldTransform);
        }

        public void DrawUnderline(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, GlyphOrientationAngle orientationAngle, IntPtr underline, object clientDrawingEffect) { }
        public void DrawStrikethrough(IntPtr clientDrawingContext, float baselineOriginX, float baselineOriginY, GlyphOrientationAngle orientationAngle, IntPtr strikethrough, object clientDrawingEffect) { }
        public void DrawInlineObject(IntPtr clientDrawingContext, float originX, float originY, GlyphOrientationAngle orientationAngle, IntPtr inlineObject, bool isSideways, bool isRightToLeft, object clientDrawingEffect) { }
    }
}
