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
        public static readonly DependencyProperty FontFeaturesProperty = DependencyProperty.Register(nameof(FontFeatures), typeof(IList<FontFeature>), typeof(DirectWriteElement), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsRender));
        public static readonly DependencyProperty ParagraphReadingDirectionProperty = DependencyProperty.Register(nameof(ParagraphReadingDirection), typeof(ReadingDirection), typeof(DirectWriteElement), new FrameworkPropertyMetadata(ReadingDirection.LeftToRight, InvalidateTextFormat));
        public static readonly DependencyProperty VerticalGlyphOrientationProperty = DependencyProperty.Register(nameof(VerticalGlyphOrientation), typeof(VerticalGlyphOrientation), typeof(DirectWriteElement), new FrameworkPropertyMetadata(VerticalGlyphOrientation.Default, InvalidateTextFormat));

        public static readonly DependencyProperty ParagraphFlowDirectionProperty = DependencyProperty.Register(nameof(ParagraphFlowDirection), typeof(Win32.DWrite.FlowDirection), typeof(DirectWriteElement), new FrameworkPropertyMetadata(Win32.DWrite.FlowDirection.TopToBottom, InvalidateTextFormat));
        public static readonly DependencyProperty TextAlignmentProperty = DependencyProperty.Register(nameof(TextAlignment), typeof(Win32.DWrite.TextAlignment), typeof(DirectWriteElement), new FrameworkPropertyMetadata(Win32.DWrite.TextAlignment.Leading, InvalidateTextFormat));
        public static readonly DependencyProperty ParagraphAlignmentProperty = DependencyProperty.Register(nameof(ParagraphAlignment), typeof(ParagraphAlignment), typeof(DirectWriteElement), new FrameworkPropertyMetadata(ParagraphAlignment.Near, InvalidateTextFormat));
        public static readonly DependencyProperty WordWrappingProperty = DependencyProperty.Register(nameof(WordWrapping), typeof(WordWrapping), typeof(DirectWriteElement), new FrameworkPropertyMetadata(WordWrapping.Wrap, InvalidateTextFormat));

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

        public IList<FontFeature> FontFeatures
        {
            get { return (IList<FontFeature>)GetValue(FontFeaturesProperty); }
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

        public VerticalGlyphOrientation VerticalGlyphOrientation
        {
            get { return (VerticalGlyphOrientation)GetValue(VerticalGlyphOrientationProperty); }
            set { SetValue(VerticalGlyphOrientationProperty, value); }
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

        protected static readonly DWrite.IDWriteFactory7 DWriteFactory;
        private static readonly DWrite.IDWriteFontFallback _noFallback;

        static DirectWriteElement()
        {
            DWriteFactory = (DWrite.IDWriteFactory7)Win32.DWrite.DWriteFactory.Shared.NativeObject;

            var fallbackBuilder = DWriteFactory.CreateFontFallbackBuilder();
            _noFallback = fallbackBuilder.CreateFontFallback();
        }

        #region TextFormat

        private TextFormat _textFormat;
        public TextFormat TextFormat
        {
            get { return _textFormat; }
            private set
            {
                if (_textFormat != value)
                {
                    _textFormat = value;
                    OnTextFormatChanged();
                }
            }
        }
        private static void InvalidateTextFormat(DependencyObject d, DependencyPropertyChangedEventArgs e) => ((DirectWriteElement)d).InvalidateTextFormat();
        private void InvalidateTextFormat()
        {
            TextFormat = null;
            InvalidateMeasure(); // needed for flow layouts
            InvalidateVisual(); // needed for fixed layouts
        }

        private TextFormat GetOrCreateTextFormat()
        {
            if (FontFace == null)
                return null;

            if (TextFormat == null)
            {
                string familyName = FontFace.TypographicFamilyName;
                FontAxisValue[] axisValues = FontAxisValues as FontAxisValue[] ?? FontAxisValues?.ToArray();

                DWrite.IDWriteFontCollection collection = null;
                if (FontSet != null)
                    collection = DWriteFactory.CreateFontCollectionFromFontSet(FontSet.NativeObject, FontFamilyModel.Typographic);

                var textFormat = DWriteFactory.CreateTextFormat(familyName, collection, axisValues, axisValues?.Length ?? 0, FontSize, LocaleName);
                textFormat.SetFontFallback(_noFallback);
                textFormat.SetFlowDirection(ParagraphFlowDirection);
                textFormat.SetReadingDirection(ParagraphReadingDirection);
                textFormat.SetTextAlignment(TextAlignment);
                textFormat.SetParagraphAlignment(ParagraphAlignment);
                textFormat.SetWordWrapping(WordWrapping);

                TextFormat = new TextFormat(textFormat);
            }

            return TextFormat;
        }

        protected virtual void OnTextFormatChanged()
        {
            TextFormatChanged?.Invoke(this, EventArgs.Empty);
        }
        public EventHandler TextFormatChanged;

        #endregion

        #region Text Layout

        private TextLayout _textLayout;
        public TextLayout TextLayout
        {
            get { return _textLayout; }
            private set
            {
                if (_textLayout != value)
                {
                    _textLayout = value;
                    OnTextLayoutChanged();
                }
            }
        }

        private TextLayout CreateTextLayout(Size size)
        {
            var textFormat = GetOrCreateTextFormat();
            if (textFormat == null)
                return null;

            var textLayout = DWriteFactory.CreateTextLayout(Text, Text?.Length ?? 0, textFormat.NativeObject, (float)size.Width, (float)size.Height);

            var wholeRange = new TextRange { Length = Text?.Length ?? 0 };
            if (FontFeatures is IEnumerable<FontFeature> features)
            {
                var typography = DWriteFactory.CreateTypography();
                foreach (var feature in features)
                    typography.AddFontFeature(feature);

                textLayout.SetTypography(typography, wholeRange);
            }

            if (textLayout is DWrite.IDWriteTextLayout2 layout2)
                layout2.SetVerticalGlyphOrientation(VerticalGlyphOrientation);

            return new TextLayout(textLayout);
        }

        public void Render(DWrite.IDWriteTextRenderer renderer)
        {
            TextLayout.NativeObject.Draw(IntPtr.Zero, renderer, 0, 0);
        }

        protected virtual void OnTextLayoutChanged()
        {
            TextLayoutChanged?.Invoke(this, EventArgs.Empty);
        }
        public EventHandler TextLayoutChanged;

        #endregion

        protected override Size MeasureOverride(Size availableSize)
        {
            try
            {
                var textLayout = CreateTextLayout(availableSize);
                if (textLayout != null)
                {
                    var metrics = textLayout.TextMetrics;
                    return new Size(Math.Ceiling(metrics.Width), Math.Ceiling(metrics.Height)); // bitmap requires integer pixels, when we switch to geometry we can remove
                }
            }
            catch { }

            return base.MeasureOverride(availableSize);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            try
            {
                TextLayout = CreateTextLayout(finalSize); // PERF: we could cache for finalSize = availableSize
                return finalSize;
            }
            catch { }

            TextLayout = null;
            return base.ArrangeOverride(finalSize);
        }
    }
}
