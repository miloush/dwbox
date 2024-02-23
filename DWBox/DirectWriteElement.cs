using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Win32.DWrite;

namespace DWBox
{
    public class DirectWriteElement : FrameworkElement
    {
        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.Register(nameof(FontSize), typeof(float), typeof(DirectWriteElement), new FrameworkPropertyMetadata(48f, InvalidateTextFormat));
        public static readonly DependencyProperty LocaleNameProperty = DependencyProperty.Register(nameof(LocaleName), typeof(string), typeof(DirectWriteElement), new FrameworkPropertyMetadata(null, InvalidateTextFormat));

        public static readonly DependencyProperty FontFaceProperty = DependencyProperty.Register(nameof(FontFace), typeof(FontFace), typeof(DirectWriteElement), new FrameworkPropertyMetadata(null, InvalidateTextFormat));
        public static readonly DependencyProperty FontAxisValuesProperty = DependencyProperty.Register(nameof(FontAxisValues), typeof(IList<FontAxisValue>), typeof(DirectWriteElement), new FrameworkPropertyMetadata(null, InvalidateTextFormat));
        public static readonly DependencyProperty FontSetProperty = DependencyProperty.Register(nameof(FontSet), typeof(FontSet), typeof(DirectWriteElement), new FrameworkPropertyMetadata(null, InvalidateTextFormat));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(DirectWriteElement), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty FontFeaturesProperty = DependencyProperty.Register(nameof(FontFeatures), typeof(IList<FontFeatureTag>), typeof(DirectWriteElement), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ParagraphReadingDirectionProperty = DependencyProperty.Register(nameof(ParagraphReadingDirection), typeof(ReadingDirection), typeof(DirectWriteElement), new FrameworkPropertyMetadata(ReadingDirection.LeftToRight, InvalidateTextFormat));
        public static readonly DependencyProperty ParagraphFlowDirectionProperty = DependencyProperty.Register(nameof(ParagraphFlowDirection), typeof(Win32.DWrite.FlowDirection), typeof(DirectWriteElement), new FrameworkPropertyMetadata(Win32.DWrite.FlowDirection.TopToBottom, InvalidateTextFormat));
        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(nameof(TextAlignment), typeof(Win32.DWrite.TextAlignment), typeof(DirectWriteElement), new FrameworkPropertyMetadata(Win32.DWrite.TextAlignment.Leading, InvalidateTextFormat));
        public static readonly DependencyProperty ParagraphAlignmentProperty = DependencyProperty.Register(nameof(ParagraphAlignment), typeof(ParagraphAlignment), typeof(DirectWriteElement), new FrameworkPropertyMetadata(ParagraphAlignment.Near, InvalidateTextFormat));
        public static readonly DependencyProperty WordWrappingProperty = DependencyProperty.Register(nameof(WordWrapping), typeof(WordWrapping), typeof(DirectWriteElement), new FrameworkPropertyMetadata(WordWrapping.Wrap, InvalidateTextFormat));

        public static readonly DependencyProperty TextAntialiasModeProperty = DependencyProperty.Register(nameof(TextAntialiasMode), typeof(TextAntialiasMode), typeof(DirectWriteElement), new FrameworkPropertyMetadata(TextAntialiasMode.ClearType, FrameworkPropertyMetadataOptions.AffectsRender, InvalidateRenderTarget));

        public TextAntialiasMode TextAntialiasMode
        {
            get { return (TextAntialiasMode)GetValue(TextAntialiasModeProperty); }
            set { SetValue(TextAntialiasModeProperty, value); }
        }

        public FontSet FontSet
        {
            get { return (FontSet)GetValue(FontSetProperty); }
            set { SetValue(FontSetProperty, value); }
        }

        public IList<FontAxisValue> FontAxisValues
        {
            get { return (FontAxisValue[])GetValue(FontAxisValuesProperty); }
            set { SetValue(FontAxisValuesProperty, value); }
        }

        public FontFace FontFace
        {
            get { return (FontFace)GetValue(FontFaceProperty); }
            set { SetValue(FontFaceProperty, value); }
        }

        public IList<FontFeatureTag> FontFeatures
        {
            get { return (IList<FontFeatureTag>)GetValue(FontFeaturesProperty); }
            set { SetValue(FontFeaturesProperty, value); }
        }

        public string LocaleName
        {
            get { return (string)GetValue(LocaleNameProperty); }
            set { SetValue(LocaleNameProperty, value); }
        }

        public WordWrapping WordWrapping
        {
            get { return (WordWrapping)GetValue(WordWrappingProperty); }
            set { SetValue(WordWrappingProperty, value); }
        }

        public ParagraphAlignment ParagraphAlignment
        {
            get { return (ParagraphAlignment)GetValue(ParagraphAlignmentProperty); }
            set { SetValue(ParagraphAlignmentProperty, value); }
        }

        public Win32.DWrite.TextAlignment TextAlignment
        {
            get { return (Win32.DWrite.TextAlignment)GetValue(TextAlignmentProperty); }
            set { SetValue(TextAlignmentProperty, value); }
        }

        public Win32.DWrite.FlowDirection ParagraphFlowDirection
        {
            get { return (Win32.DWrite.FlowDirection)GetValue(ParagraphFlowDirectionProperty); }
            set { SetValue(ParagraphFlowDirectionProperty, value); }
        }

        public ReadingDirection ParagraphReadingDirection
        {
            get { return (ReadingDirection)GetValue(ParagraphReadingDirectionProperty); }
            set { SetValue(ParagraphReadingDirectionProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public float FontSize
        {
            get { return (float)GetValue(FontSizeProperty); }
            set { SetValue(FontSizeProperty, value); }
        }

        public DWrite.IDWriteTextRenderer AdditionalRenderer;

        private static readonly DWrite.IDWriteFactory7 _factory;
        private static readonly DWrite.IDWriteGdiInterop _gdiInterop;
        private static readonly DWrite.IDWriteFontFallback _noFallback;
        private DWrite.IDWriteBitmapRenderTarget _renderTarget;
        private BitmapRenderer _renderer;
        private BitmapSource _bitmap;
        private PixelFormat _bitmapFormat = PixelFormats.Bgr32;
        private IntPtr hBitmapData;

        private DpiScale _dpiScale = new DpiScale(1, 1);
        public DpiScale DpiScale => _dpiScale;
        protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) => _dpiScale = newDpi;

        static DirectWriteElement()
        {
            _factory = DWriteFactory.Shared7;
            _gdiInterop = _factory.GetGdiInterop();

            var fallbackBuilder = _factory.CreateFontFallbackBuilder();
            _noFallback = fallbackBuilder.CreateFontFallback();
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            if (PresentationSource.FromVisual(this)?.CompositionTarget?.TransformToDevice is System.Windows.Media.Matrix matrix)
                _dpiScale = new DpiScale(matrix.M11, matrix.M22);
        }

        public void Render(DWrite.IDWriteTextRenderer renderer) => OnRender(null, renderer);
        protected override void OnRender(DrawingContext drawingContext)
        {
            int scaledWidth = (int)(RenderSize.Width * _dpiScale.DpiScaleX);
            int scaledHeight = (int)(RenderSize.Height * _dpiScale.DpiScaleY);

            EnsureRenderTarget((uint)scaledWidth, (uint)scaledHeight);
            OnRender(drawingContext, _renderer);
        }

        private DWrite.IDWriteTextFormat _textFormat;
        private void InvalidateTextFormat()
        {
            _textFormat = null;
            InvalidateVisual();
        }
        private static void InvalidateTextFormat(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DirectWriteElement)d).InvalidateTextFormat();
        private DWrite.IDWriteTextFormat GetOrCreateTextFormat()
        {
            if (_textFormat == null)
            {
                string familyName = FontFace.TypographicFamilyName;

                FontAxisValue[] axisValues = FontAxisValues as FontAxisValue[] ?? FontAxisValues?.ToArray();

                DWrite.IDWriteFontCollection collection = null;
                if (FontSet != null)
                    collection = _factory.CreateFontCollectionFromFontSet(FontSet.NativeObject, FontFamilyModel.Typographic);

                var textFormat = _factory.CreateTextFormat(familyName, collection, axisValues, axisValues?.Length ?? 0, FontSize, LocaleName);
                textFormat.SetFontFallback(_noFallback);
                textFormat.SetFlowDirection(ParagraphFlowDirection);
                textFormat.SetReadingDirection(ParagraphReadingDirection);
                textFormat.SetTextAlignment(TextAlignment);
                textFormat.SetParagraphAlignment(ParagraphAlignment);
                textFormat.SetWordWrapping(WordWrapping);
                
                _textFormat = textFormat;
            }

            return _textFormat;
        }

        private DWrite.IDWriteTextLayout _textLayout;
        private DWrite.IDWriteTextLayout CreateTextLayout(Size size)
        {
            var textFormat = GetOrCreateTextFormat();
            var textLayout = _factory.CreateTextLayout(Text, Text?.Length ?? 0, textFormat, (float)size.Width, (float)size.Height);

            var wholeRange = new TextRange { Length = Text?.Length ?? 0 };
            if (FontFeatures is IEnumerable<FontFeatureTag> features)
            {
                var typography = _factory.CreateTypography();
                foreach (var feature in features)
                    typography.AddFontFeature(new FontFeature { NameTag = feature, Parameter = 1 });

                textLayout.SetTypography(typography, wholeRange);
            }

            return textLayout;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            try
            {
                var textLayout = CreateTextLayout(availableSize);
                var metrics = textLayout.GetMetrics();

                return new Size(Math.Ceiling(metrics.Width), Math.Ceiling(metrics.Height)); // bitmap requires integer pixels, when we switch to geometry we can remove
            }
            catch
            {
                return base.MeasureOverride(availableSize);
            }
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            try
            {
                _textLayout = CreateTextLayout(finalSize);
                return finalSize;
            }
            catch
            {
                _textLayout = null;
                return base.ArrangeOverride(finalSize);
            }
        }

        private void OnRender(DrawingContext drawingContext, DWrite.IDWriteTextRenderer renderer)
        {
            if (_textLayout == null)
                return;

            int width = (int)RenderSize.Width;
            int height = (int)RenderSize.Height;
            int scaledWidth = (int)(RenderSize.Width * _dpiScale.DpiScaleX);
            int scaledHeight = (int)(RenderSize.Height * _dpiScale.DpiScaleY);

            try
            {
                _textLayout.Draw(IntPtr.Zero, renderer, 0, 0);

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
            if (_textLayout == null)
                return null;

            var metrics = _textLayout.GetMetrics();
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
        private static void InvalidateRenderTarget(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DirectWriteElement)d).InvalidateRenderTarget();
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

                _renderer = new BitmapRenderer(_renderTarget, _factory.CreateRenderingParams());
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
