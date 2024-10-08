﻿using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Win32.DWrite;

namespace DWBox
{
    public class BoxItem : DependencyObject
    {
        public static readonly DependencyProperty EmSizeProperty = DependencyProperty.Register(nameof(EmSize), typeof(float), typeof(BoxItem), new PropertyMetadata(48f));
        public static readonly DependencyProperty HeaderBrushProperty = DependencyProperty.Register(nameof(HeaderBrush), typeof(Brush), typeof(BoxItem), new PropertyMetadata(Brushes.WhiteSmoke));
        public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register(nameof(BorderBrush), typeof(Brush), typeof(BoxItem), new PropertyMetadata(Brushes.Silver));

        public float EmSize
        {
            get { return (float)GetValue(EmSizeProperty); }
            set { SetValue(EmSizeProperty, value); }
        }

        public Brush HeaderBrush
        {
            get { return (Brush)GetValue(HeaderBrushProperty); }
            set { SetValue(HeaderBrushProperty, value); }
        }

        public Brush BorderBrush
        {
            get { return (Brush)GetValue(BorderBrushProperty); }
            set { SetValue(BorderBrushProperty, value); }
        }

        public BoxItem(FontSetEntry entry)
        {
            FontSetEntry = entry;
        }

        public FontSetEntry FontSetEntry { get; }
        public FontSet FontSet => FontSetEntry.FontSet;
        public string Name => FontSetEntry.FullName;
        public string TypographicFamilyName => FontSetEntry.TypographicFamilyName;

        private FontSet _singleFontSet;
        public FontSet SingleFontSet
        {
            get
            {
                if (_singleFontSet == null)
                {
                    if (FontSet.Count == 1)
                        return _singleFontSet = FontSet;
                    else
                    {
                        var builder = DWriteFactory.Shared6.CreateFontSetBuilder2();
                        var reference = FontSetEntry.NativeObject.GetFontFaceReference(FontSetEntry.Index);
                        builder.AddFontFaceReference(reference);

                        _singleFontSet = new FontSet(builder.CreateFontSet());
                    }
                }

                return _singleFontSet;
            }
        }

        private FontFace _fontFace;
        public FontFace FontFace => _fontFace ??= FontSetEntry.CreateFontFace();
        public FontAxisValue[] FontAxisValues => FontFace.GetFontAxisValues();

        private FontResource _fontResource;
        public FontResource FontResource => _fontResource ??= FontSetEntry.CreateFontResource();

        private FontFile _fontFile;
        public FontFile FontFile => _fontFile ??= FontResource.GetFontFile();

        public string FilePath
        {
            get
            {
                var resource = FontSetEntry.CreateFontResource();
                var file = resource.GetFontFile();

                file.NativeObject.GetReferenceKey(out IntPtr keyData, out int keyLength);

                try
                {
                    var loader = (DWrite.IDWriteLocalFontFileLoader)file.NativeObject.GetLoader();
                    int pathLength = loader.GetFilePathLengthFromKey(keyData, keyLength);
                    StringBuilder path = new StringBuilder(pathLength + 1);
                    loader.GetFilePathFromKey(keyData, keyLength, path, path.Capacity);
                    return path.ToString();
                }
                catch (InvalidCastException)
                {
                    return null;
                }
            }
        }

        private FileInfo _fileInfo;
        public FileInfo FileInfo => _fileInfo ??= new FileInfo(FilePath);

        public string Version => FontFace.Version;
        public string NameVersion
        {
            get
            {
                if (Version is string version)
                {
                    if (version.StartsWith("Version ", System.StringComparison.OrdinalIgnoreCase))
                        return string.Join(" ", Name, version.Substring("Version ".Length));
                }

                return Name;
            }
        }

        public ImageSource SourceTypeImage
        {
            get
            {
                return FontSetEntry.FontSourceType switch
                {
                    FontSourceType.Unknown => (ImageSource)Application.Current.FindResource("FileDestination"),
                    FontSourceType.PerMachine => (ImageSource)Application.Current.FindResource("LocalServer"),
                    FontSourceType.PerUser => (ImageSource)Application.Current.FindResource("User"),
                    FontSourceType.AppxPackage => (ImageSource)Application.Current.FindResource("Package"),
                    FontSourceType.RemoteFontProvider => (ImageSource)Application.Current.FindResource("Cloud"),
                    _ => null,
                };
            }
        }

        private DirectWriteElement _el;
        public DirectWriteElement RenderingElement { get { return _el; } set { _el = value; } }

        // Attached

        public static readonly DependencyProperty OwningItemProperty = DependencyProperty.RegisterAttached("OwningItem", typeof(BoxItem), typeof(BoxItem), new PropertyMetadata(null, OnOwningItemChanged));

        private static void OnOwningItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            SetOwningItem((DirectWriteElement)d, (BoxItem)e.NewValue);
        }

        public static BoxItem GetOwningItem(DirectWriteElement obj)
        {
            return obj.DataContext as BoxItem;
        }
        public static void SetOwningItem(DirectWriteElement obj, BoxItem value)
        {
            value.RenderingElement = obj;
        }

    }
}