using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Win32.DWrite;

namespace DWBox
{
    public class DirectWriteBitmapElement : DirectWriteElement
    {
        public static readonly DependencyProperty TextAntialiasModeProperty = DependencyProperty.Register(nameof(TextAntialiasMode), typeof(TextAntialiasMode), typeof(DirectWriteBitmapElement), new FrameworkPropertyMetadata(TextAntialiasMode.ClearType, FrameworkPropertyMetadataOptions.AffectsRender, InvalidateRenderTarget));

        public TextAntialiasMode TextAntialiasMode
        {
            get { return (TextAntialiasMode)GetValue(TextAntialiasModeProperty); }
            set { SetValue(TextAntialiasModeProperty, value); }
        }

        private static readonly DWrite.IDWriteGdiInterop _gdiInterop;

        private DWrite.IDWriteBitmapRenderTarget _renderTarget;
        private BitmapRenderer _renderer;
        private BitmapSource _bitmap;
        private PixelFormat _bitmapFormat = PixelFormats.Bgr32;
        private IntPtr hBitmapData;

        static DirectWriteBitmapElement()
        {
            _gdiInterop = DWriteFactory.GetGdiInterop();
        }

        private DpiScale _dpiScale = new DpiScale(1, 1);
        public DpiScale DpiScale => _dpiScale;
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) => _dpiScale = newDpi;

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);

            if (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice is System.Windows.Media.Matrix matrix)
                _dpiScale = new DpiScale(matrix.M11, matrix.M22);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            int scaledWidth = (int)(RenderSize.Width * _dpiScale.DpiScaleX);
            int scaledHeight = (int)(RenderSize.Height * _dpiScale.DpiScaleY);

            EnsureRenderTarget((uint)scaledWidth, (uint)scaledHeight);
            OnRender(drawingContext, _renderer);
        }

        private void OnRender(DrawingContext drawingContext, DWrite.IDWriteTextRenderer renderer)
        {
            if (TextLayout == null)
                return;

            int width = (int)RenderSize.Width;
            int height = (int)RenderSize.Height;
            int scaledWidth = (int)(RenderSize.Width * _dpiScale.DpiScaleX);
            int scaledHeight = (int)(RenderSize.Height * _dpiScale.DpiScaleY);

            try
            {
                Render(renderer);

                if (drawingContext != null && hBitmapData != IntPtr.Zero)
                {
                    _bitmap = BitmapSource.Create(scaledWidth, scaledHeight, 96, 96, _bitmapFormat, null, hBitmapData, scaledWidth * scaledHeight * _bitmapFormat.BitsPerPixel / 8, scaledWidth * _bitmapFormat.BitsPerPixel / 8);
                    drawingContext.DrawImage(_bitmap, new Rect(0, 0, width, height));
                }
            }
            catch (Exception e)
            {
                if (drawingContext == null)
                    throw;

                drawingContext.DrawText(new FormattedText(e.Message, CultureInfo.CurrentUICulture, FlowDirection, new Typeface("Segoe UI"), 11, Brushes.Red, _dpiScale.PixelsPerDip) { MaxTextWidth = width }, default);
            }
        }

        internal BitmapSource GetLastRenderedBoundingBitmap()
        {
            if (TextLayout == null)
                return null;

            var metrics = TextLayout.TextMetrics;
            int left = (int)(metrics.Left * _dpiScale.DpiScaleX);
            int top = (int)(metrics.Top * _dpiScale.DpiScaleY);
            int width = (int)Math.Ceiling(metrics.Width * _dpiScale.DpiScaleX);
            int height = (int)Math.Ceiling(metrics.Height * _dpiScale.DpiScaleY);

            Int32Rect boundingRect = new Int32Rect(left, top, Math.Min(_bitmap.PixelWidth - left, width), Math.Min(_bitmap.PixelHeight - top, height));
            return new CroppedBitmap(_bitmap, boundingRect);
        }
        internal BitmapSource GetLastRenderedBitmap()
        {
            return _bitmap;
        }

        private void InvalidateRenderTarget()
        {
            _renderTarget = null;
        }
        private static void InvalidateRenderTarget(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DirectWriteBitmapElement)d).InvalidateRenderTarget();
        private void EnsureRenderTarget(uint width, uint height)
        {
            if (_renderTarget == null)
            {
                _renderTarget = _gdiInterop.CreateBitmapRenderTarget(IntPtr.Zero, width, height);
                _bitmapFormat = PixelFormats.Bgr32;
                if (TextAntialiasMode == TextAntialiasMode.Grayscale)
                    if (_renderTarget is DWrite.IDWriteBitmapRenderTarget1 target1)
                    {
                        target1.SetTextAntialiasMode(TextAntialiasMode.Grayscale);
                        _bitmapFormat = PixelFormats.Pbgra32;
                    }

                _renderer = new BitmapRenderer(_renderTarget, DWriteFactory.CreateRenderingParams());
            }
            else
            {
                _renderTarget.Resize(width, height);
            }
            IntPtr hdc = _renderTarget.GetMemoryDC();
            IntPtr hBitmap = GetCurrentObject(hdc, 7);

            GetObjectW(hBitmap, Marshal.SizeOf<tagBITMAP>(), out tagBITMAP bm);
            hBitmapData = bm.bmBits == IntPtr.Zero ? IntPtr.Zero : bm.bmBits;

            if (hBitmapData != IntPtr.Zero)
            {
                // fill white              
                int pixels = bm.bmWidth * bm.bmHeight;
                int color = _bitmapFormat == PixelFormats.Pbgra32 ? default : 0x00FFFFFF;

                for (int i = 0; i < pixels; i++)
                    Marshal.WriteInt32(hBitmapData, i * 4, color);
            }
        }

        [DllImport("gdi32.dll")]
        private static extern IntPtr GetCurrentObject(IntPtr hdc, int objectType);

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern int GetObjectW(IntPtr h, int c, out tagBITMAP pv);

        [DllImport("gdi32.dll", SetLastError = true)]
        static extern int GetObjectW(IntPtr h, int c, IntPtr pv);

        [StructLayout(LayoutKind.Sequential, Size = 0x20)]
        struct tagBITMAP
        {
            public int bmType;
            public int bmWidth;
            public int bmHeight;
            public int bmWidthBytes;
            public short bmPlanes;
            public short bmBitsPixel;
            public IntPtr bmBits;
        }
    }
}
