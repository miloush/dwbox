using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using DWBox.Properties;
using Win32.DWrite;

namespace DWBox
{
    public partial class MainWindow : Window
    {
        private readonly BoxItemCollection _items = new BoxItemCollection();

        public MainWindow()
        {
            InitializeComponent();
            _renderings.ItemsSource = _items.View;

            try
            {
                _boxInput.Text = Settings.Default.LastInput;
                AddEmSize = Settings.Default.LastAddedSize;
            }
            catch { }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _modules.ItemsSource = from module in Process.GetCurrentProcess().Modules.OfType<ProcessModule>()
                                   where module.ModuleName.StartsWith("dwrite", StringComparison.OrdinalIgnoreCase) || module.ModuleName.StartsWith("textshaping", StringComparison.OrdinalIgnoreCase)
                                   select new { module.ModuleName, module.FileVersionInfo, FileInfo = new FileInfo(module.FileName) };

            ListCollectionView view = new ListCollectionView(CultureInfo.GetCultures(CultureTypes.AllCultures));
            view.GroupDescriptions.Add(new CultureGroupDescription());
            _boxLocale.ItemsSource = view;

            RefreshSystemFonts(sender, e);

            TaskbarItemInfo info = new TaskbarItemInfo();
            info.ThumbButtonInfos.Add(CreateThumbButton(Brushes.Transparent));
            info.ThumbButtonInfos.Add(CreateThumbButton(new SolidColorBrush(Color.FromRgb(242, 80, 34))));
            info.ThumbButtonInfos.Add(CreateThumbButton(new SolidColorBrush(Color.FromRgb(127, 186, 2))));
            info.ThumbButtonInfos.Add(CreateThumbButton(new SolidColorBrush(Color.FromRgb(1, 165, 239))));
            //info.ThumbButtonInfos.Add(CreateThumbButton(new SolidColorBrush(Color.FromRgb(254, 185, 3))));
            TaskbarItemInfo = info;
        }

        class CultureGroupDescription : GroupDescription
        {
            public override object GroupNameFromItem(object item, int level, CultureInfo format)
            {
                CultureInfo culture = (CultureInfo)item;
                while (culture.Parent is CultureInfo parent && parent.Name != "")
                {
                    if (culture == parent)
                        break;
                    else
                        culture = parent;
                }

                return culture.EnglishName;
            }
        }

        private ThumbButtonInfo CreateThumbButton(Brush brush)
        {
            ThumbButtonInfo info = new ThumbButtonInfo();

            Geometry square = Geometry.Parse("M0,0 H32 V32 Z");
            square.Freeze();
            info.ImageSource = new DrawingImage { Drawing = new GeometryDrawing(brush, null, square) };
            info.Click += OnInstanceColorChanged;
            return info;
        }

        private void OnInstanceColorChanged(object sender, EventArgs e)
        {
            if (sender is ThumbButtonInfo info && info.ImageSource is ImageSource overlay)
            {
                TaskbarItemInfo.Overlay = overlay;
                if (overlay is DrawingImage image && image.Drawing is GeometryDrawing drawing)
                    Application.Current.Resources["InstanceBrush"] = drawing.Brush;
            }
        }

        private void RefreshSystemFonts(object sender, EventArgs e)
        {
            if (sender is FontSet oldSet)
                oldSet.Expired -= RefreshSystemFonts;

            var fontset = new FontSet(DWriteFactory.Shared6.GetSystemFontSet(includeDownloadableFonts: false));
            fontset.Expired += RefreshSystemFonts;

            Dispatcher.BeginInvoke(ApplyAndSelect, fontset);
        }
        private void ApplyAndSelect(FontSet fontset)
        {
            ListCollectionView view = new ListCollectionView(fontset);
            view.SortDescriptions.Add(new SortDescription(nameof(FontSetEntry.FullName), ListSortDirection.Ascending));
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(FontSetEntry.TypographicFamilyName)));

            string selectedName = (_fontSelector.SelectedItem as FontSetEntry)?.FullName;
            string lastName = Settings.Default.LastAddedFont;

            FontSetEntry bestEntry = null;
            foreach (FontSetEntry entry in fontset)
            {
                if (bestEntry == null && entry.FullName == "Segoe UI")
                    bestEntry = entry;

                if (entry.FullName == lastName)
                    bestEntry = entry;

                if (entry.FullName == selectedName)
                {
                    bestEntry = entry;
                    break;
                }
            }

            _fontSelector.ItemsSource = view;
            _fontSelector.SelectedItem = bestEntry ?? fontset.FirstOrDefault();
        }

        private void OnAdd(object sender, RoutedEventArgs e)
        {
            if (_fontSelector.SelectedItem is FontSetEntry entry)
            {
                _items.Add(entry, AddEmSize);
                Settings.Default.LastAddedFont = entry.FullName;
                Settings.Default.LastAddedSize = AddEmSize;
            }

            try { Settings.Default.Save(); }
            catch { }
        }

        private async void OnAddInput(object sender, RoutedEventArgs e)
        {
            try { Settings.Default.Save(); }
            catch { }

            List<FontSetEntry> entries = new List<FontSetEntry>();
            try
            {
                Cursor = Cursors.Wait;
                await GetMatchingEntries(entries);
            }
            finally
            {
                ClearValue(CursorProperty);
            }

            if (entries.Count < 1)
            {
                TaskDialog.Show(this, "No fonts containing requested characters found.", Title, "Add fonts", null, TaskDialogButtons.OK, TaskDialogImage.Warning, options: TaskDialogOptions.CenterOwner);
                return;
            }
            else if (entries.Count == 1)
            {
                if (TaskDialog.Show(this, $"Only single font '{entries[0].FullName}' contains the requested characters.", Title, "Add fonts", TaskDialogButtons.OKCancel, TaskDialogImage.Information) == TaskDialogButtonResult.OK)
                    _items.Add(entries[0], AddEmSize);
                return;
            }
            else
            {
                HashSet<string> families = new(entries.Select(e => e.TypographicFamilyName));
                SortedList<string, FontSetEntry> bestPerFamily = new SortedList<string, FontSetEntry>(families.Count);

                var result = TaskDialog.Show(this, $"Found {entries.Count} matching font faces belonging to {families.Count} typographic families.", Title, "Add fonts", null, TaskDialogButtons.Cancel, TaskDialogImage.Information, [$"Add all {entries.Count} font faces", "Add one font face per family"]);

                switch (result.CustomButtonIndex)
                {
                    case 0:
                        foreach (var entry in entries)
                            _items.Add(entry, AddEmSize);
                        break;

                    case 1:
                        foreach (var entry in entries)
                            if (!bestPerFamily.TryGetValue(entry.TypographicFamilyName, out var currentBest) || IsBetter(entry, currentBest))
                                bestPerFamily[entry.TypographicFamilyName] = entry;

                        foreach (var entry in bestPerFamily.Values)
                            _items.Add(entry, AddEmSize);
                        
                        break;

                    default:
                        return;
                }
            }
        }

        private static bool IsBetter(FontSetEntry proposed, FontSetEntry current)
        {
            var aValues = current.GetFontFaceReference().GetFontAxisValues().ToDictionary(av => av.AxisTag, av => av.Value);
            var bValues = proposed.GetFontFaceReference().GetFontAxisValues().ToDictionary(av => av.AxisTag, av => av.Value);

            if (aValues.TryGetValue(FontAxisTag.Weight, out var aWeight) && bValues.TryGetValue(FontAxisTag.Weight, out var bWeight))
                if (aWeight != bWeight)
                    return bWeight == (float)Win32.DWrite.FontWeight.Regular;

            if (aValues.TryGetValue(FontAxisTag.Italic, out var aItalic) && bValues.TryGetValue(FontAxisTag.Italic, out var bItalic))
                if (aItalic != bItalic)
                    return bItalic < aItalic;

            if (aValues.TryGetValue(FontAxisTag.Width, out var aWidth) && bValues.TryGetValue(FontAxisTag.Width, out var bWidth))
                if (aWidth != bWidth)
                    return Math.Abs(bWidth - 100) < Math.Abs(aWidth - 100);

            if (aValues.TryGetValue(FontAxisTag.Slant, out var aSlant) && bValues.TryGetValue(FontAxisTag.Slant, out var bSlant))
                if (aSlant != bSlant)
                    return Math.Abs(bSlant) < Math.Abs(aSlant);

            return false;
        }

        private void OnAddFamily(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el)
            {
                BoxItem item = (BoxItem)el.DataContext;
                string familyName = item.TypographicFamilyName;

                if (string.IsNullOrEmpty(familyName))
                {
                    TaskDialog.Show(this, "This font does not have a typographic family name set.", Title, "Add font family", TaskDialogButtons.OK, TaskDialogImage.Error);
                    return;
                }

                bool added = false;
                var fontset = new FontSet(DWriteFactory.Shared6.GetSystemFontSet(includeDownloadableFonts: false));
                foreach (var entry in fontset)
                    if (entry.TypographicFamilyName == familyName)
                        added |= _items.Add(entry, item.EmSize);

                if (!added)
                    TaskDialog.Show(this, "No other fonts of the same typographic family found in the system font set.", Title, "Add font family", TaskDialogButtons.OK, TaskDialogImage.Information);
            }
        }


        Task GetMatchingEntries(IList<FontSetEntry> entries)
        {
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;

            int[] codepoints = ToCodepoints(_boxOutput.Text).ToArray();
            ushort[] glyphs = new ushort[codepoints.Length];

            var set = DWriteFactory.Shared6.GetSystemFontSet(includeDownloadableFonts: false);
            int count = set.GetFontCount();

            TaskbarItemInfo.ProgressState = 0;
            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;

            for (int i = 0; i < count; i++)
            {
                try
                {
                    var face = set.CreateFontFace(i);
                    face.GetGlyphIndices(codepoints, codepoints.Length, glyphs);

                    if (Array.IndexOf(glyphs, (ushort)0) < 0)
                        entries.Add(new FontSetEntry(set, i));
                }
                catch (COMException) { }

                if ((i % 100) == 0)
                    TaskbarItemInfo.ProgressValue = (double)i / count;
            }

            TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;

            return Task.FromResult(entries);
        }

        private void OnRemove(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el)
            {
                BoxItem removingItem = (BoxItem)el.DataContext;

                if (el.Tag is string tag)
                    switch (tag)
                    {
                        case "T":
                            break;

                        case "B":
                            _items.Remove(item => item != removingItem);
                            return;

                        case "F":
                            _items.Remove(item => item.TypographicFamilyName == removingItem.TypographicFamilyName);
                            return;

                        case "M":
                            _items.Remove(item => item.FontSetEntry.FontSourceType == FontSourceType.PerMachine);
                            return;

                        case "U":
                            _items.Remove(item => item.FontSetEntry.FontSourceType == FontSourceType.PerUser);
                            return;

                        case "D":
                            _items.Remove(item => item.FontSetEntry.FontSourceType == FontSourceType.Unknown);
                            return;

                        case "A":
                            _items.Clear();
                            return;

                        case "G":
                            int[] codepoints = ToCodepoints(_boxOutput.Text).ToArray();
                            ushort[] glyphs = new ushort[codepoints.Length];
                            _items.Remove(item => Array.IndexOf(item.FontFace.GetGlyphIndices(codepoints), (ushort)0) >= 0);
                            return;
                    }

                _items.Remove(removingItem);
            }
        }

        private void InvalidateOutput(object sender, RoutedEventArgs e)
        {
            if (_boxOutput != null)
            {
                _boxOutput.Text = _decode.IsChecked == true ? Decode(_boxInput.Text) : _boxInput.Text;

                Settings.Default.LastInput = _boxInput.Text;
            }
        }

        private void OnRenderingsDragOver(object sender, DragEventArgs e)
        {
            string[] formats = e.Data.GetFormats();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
        }

        private void OnRenderingsDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop, false) is string[] paths)
            {
                try { Settings.Default.Save(); }
                catch { }

                SortedList<string, DWrite.Result> errors = new();
                SortedSet<string> duplicates = new();

                var builder = DWriteFactory.Shared6.CreateFontSetBuilder2();
                foreach (string path in paths)
                {
                    DWrite.Result result = builder.AddFontFile(path);
                    if (result != DWrite.Result.OK)
                        errors[path] = result;
                }

                var set = new FontSet(builder.CreateFontSet());

                foreach (var entry in set)
                    if (!_items.Add(entry, AddEmSize))
                    {
                        string name = entry.FullName;
                        if (entry.CreateFontResource().GetFontFile().TryGetLocalFilePath(out string path))
                            name += $" [{path}]";

                        duplicates.Add(name);
                    }

                if (errors.Count > 0 || duplicates.Count > 0)
                {
                    StringBuilder msg = new StringBuilder();
                    if (errors.Count > 0)
                    {
                        msg.AppendLine("The following files could not be loaded:");
                        foreach (var error in errors)
                            msg.AppendLine($"\u00a0-\u00a0{error.Key} [{error.Value}]");

                        msg.AppendLine();
                    }

                    if (duplicates.Count > 0)
                    {
                        msg.AppendLine("The following font faces are already present:");
                        foreach (var duplicate in duplicates)
                            msg.AppendLine("\u00a0-\u00a0" + duplicate);
                    }

                    TaskDialog.Show(this, msg.ToString(), Title, "Some fonts were not added.", null, TaskDialogButtons.OK, TaskDialogImage.Warning, null, TaskDialogOptions.SizeToContent);
                }
            }
        }

        private void OnItemMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                if (sender is FrameworkElement el)
                    if (el.DataContext is BoxItem item && item.FilePath is string path)
                    {
                        DataObject data = new DataObject();
                        data.SetFileDropList(new StringCollection { path });
                        DragDrop.DoDragDrop(el, data, DragDropEffects.Copy);
                    }
        }

        private void OnTextAnalysis(object sender, RoutedEventArgs e)
        {
            new TextAnalysisWindow(this).Show();
        }

        private void OnGlyphRunDetails(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: BoxItem item })
                if (item.RenderingElement != null)
                    new GlyphRunWindow(item).Show();
        }

        #region Clipboard

        private void CopyBitmap(BitmapSource bitmap)
        {
            PngBitmapEncoder png = new PngBitmapEncoder();
            png.Frames.Add(BitmapFrame.Create(bitmap));

            MemoryStream pngStream = new MemoryStream();
            png.Save(pngStream);
            pngStream.Seek(0, SeekOrigin.Begin);

            if (bitmap.Format == PixelFormats.Pbgra32)
            {
                // clipboard does not support transparent bitmaps, all pixels are black, alpha varies
                byte[] bitmapData = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
                bitmap.CopyPixels(bitmapData, bitmap.PixelWidth * 4, 0);

                for (int i = 0; i < bitmapData.Length; i += 4)
                {
                    byte a = (byte)(255 - bitmapData[i + 3]);
                    bitmapData[i + 0] = a;
                    bitmapData[i + 1] = a;
                    bitmapData[i + 2] = a;
                }

                bitmap = BitmapSource.Create(bitmap.PixelWidth, bitmap.PixelHeight, bitmap.DpiX, bitmap.DpiY, PixelFormats.Bgr32, null, bitmapData, bitmap.PixelWidth * 4);
            }

            DataObject data = new DataObject();
            data.SetData("PNG", pngStream);
            data.SetImage(bitmap);

            Clipboard.SetDataObject(data);
        }

        private void OnCopyBitmap(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: BoxItem item })
                if (ItemsControl.ContainerFromElement(_renderings, item.RenderingElement) is FrameworkElement el)
                {
                    Rect bounds = new Rect(default, el.RenderSize);

                    DrawingVisual visual = new DrawingVisual();
                    using (DrawingContext context = visual.RenderOpen())
                        context.DrawRectangle(new VisualBrush(el), null, bounds);

                    RenderTargetBitmap bitmap = new RenderTargetBitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height), 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(visual);

                    CopyBitmap(bitmap);
                }
        }

        private void OnCopyName(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: BoxItem item })
                Clipboard.SetText(item.NameVersion);
        }

        private void OnCopyPath(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: BoxItem item })
                Clipboard.SetText(item.FilePath);
        }

        private void OnCopyGlyphRunBitmap(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: BoxItem item })
            {
                BitmapSource bitmap = null;

                if (Keyboard.Modifiers == ModifierKeys.Shift)
                    bitmap = item.RenderingElement?.GetLastRenderedBitmap();
                else
                    bitmap = item.RenderingElement?.GetLastRenderedBoundingBitmap();

                if (bitmap != null)
                    CopyBitmap(bitmap);
            }
        }

        #endregion

        #region Font Size

        public static readonly DependencyProperty AddEmSizeProperty = DependencyProperty.Register(nameof(AddEmSize), typeof(float), typeof(MainWindow), new PropertyMetadata(48f));

        public float AddEmSize
        {
            get { return (float)GetValue(AddEmSizeProperty); }
            set { SetValue(AddEmSizeProperty, value); }
        }

        private void OnSizeKeyDown(object sender, KeyEventArgs e)
        {
            bool handled = true;
            switch (e.Key)
            {
                case Key.Up: AddEmSize++; break;
                case Key.Down: AddEmSize--; break;
                case Key.PageUp: AddEmSize = (float)Math.Ceiling(AddEmSize * 1.5); break;
                case Key.PageDown: AddEmSize = (float)Math.Ceiling(AddEmSize / 1.5); break;
                default: handled = false; break;
            }

            if (handled)
                _sizeBox.SelectAll();

            e.Handled |= handled;
        }

        private void OnSetSize(object sender, RoutedEventArgs e)
        {
            if (float.TryParse(_sizeBox.Text, out float em))
                foreach (BoxItem item in _items)
                {
                    item.EmSize = em;

                    Settings.Default.LastAddedSize = em;
                    try { Settings.Default.Save(); }
                    catch { }
                }
        }

        #endregion

        #region Layout

        private GroupStyle SelectRenderingsGroupStyle(CollectionViewGroup group, int level)
        {
            // returning null still evaluates ItemsControl.GroupStyle so it has to be stored elsewhere
            if (_groupBy.IsChecked == true)
                return _renderings.FindResource("GroupStyle") as GroupStyle;

            return null;
        }

        private void RefreshItems(object sender, RoutedEventArgs e)
        {
            if (_renderings.ItemsSource is ICollectionView view)
                view.Refresh();
        }

        private void OnRenderingsPanelChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_renderings?.IsGrouping == true)
                RefreshItems(sender, e); // this is called automatically when not grouping; bug?
        }

        private void OnCollapseAll(object sender, RoutedEventArgs e)
        {
            if (_renderings.IsGrouping)
                foreach (CollectionViewGroup groupView in _renderings.ItemContainerGenerator.Items)
                    if (_renderings.ItemContainerGenerator.ContainerFromItem(groupView) is GroupItem groupItem)
                        if (VisualTreeHelper.GetChildrenCount(groupItem) > 0 && VisualTreeHelper.GetChild(groupItem, 0) is Expander expander)
                            expander.IsExpanded = false;
        }

        private void OnExpandAll(object sender, RoutedEventArgs e)
        {
            if (_renderings.IsGrouping)
                foreach (CollectionViewGroup groupView in _renderings.ItemContainerGenerator.Items)
                    if (_renderings.ItemContainerGenerator.ContainerFromItem(groupView) is GroupItem groupItem)
                        if (VisualTreeHelper.GetChildrenCount(groupItem) > 0 && VisualTreeHelper.GetChild(groupItem, 0) is Expander expander)
                            expander.IsExpanded = true;
        }

        #endregion

        private void OpenContextMenu(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { ContextMenu: ContextMenu menu })
            {
                menu.PlacementTarget = sender as UIElement;
                menu.IsOpen = true;
            }
        }

        private static readonly Brush RedHeaderBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0xF0, 0xF0));
        private static readonly Brush RedBorderBrush = Brushes.DarkRed;

        private static readonly Brush GreenHeaderBrush = new SolidColorBrush(Color.FromRgb(0xF0, 0xFF, 0xF0));
        private static readonly Brush GreenBorderBrush = Brushes.DarkGreen;

        private void OnHighlight(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement el)
            {
                BoxItem targetItem = (BoxItem)el.DataContext;

                if (el.Tag is string tag)
                    switch (tag)
                    {
                        case "X":
                            targetItem.ClearValue(BoxItem.HeaderBrushProperty);
                            targetItem.ClearValue(BoxItem.BorderBrushProperty);
                            return;

                        case "XA":
                            foreach (var item in _items)
                            {
                                item.ClearValue(BoxItem.HeaderBrushProperty);
                                item.ClearValue(BoxItem.BorderBrushProperty);
                            }
                            return;

                        case "R":
                            targetItem.HeaderBrush = RedHeaderBrush;
                            targetItem.BorderBrush = RedBorderBrush;
                            return;
                        
                        case "G":
                            targetItem.HeaderBrush = GreenHeaderBrush;
                            targetItem.BorderBrush = GreenBorderBrush;
                            return;
                    }
            }
        }
    }
}
