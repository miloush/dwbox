using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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

            if (MessageBox.Show(this, $"Add {entries.Count} fonts?", Title, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                foreach (var entry in entries)
                    _items.Add(entry, AddEmSize);
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
                var face = set.CreateFontFace(i);
                face.GetGlyphIndices(codepoints, codepoints.Length, glyphs);

                if (Array.IndexOf(glyphs, (ushort)0) < 0)
                    entries.Add(new FontSetEntry(set, i));

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

                var builder = DWriteFactory.Shared6.CreateFontSetBuilder2();
                foreach (string path in paths)
                    builder.AddFontFile(path);

                var set = new FontSet(builder.CreateFontSet());

                foreach (var entry in set)
                    _items.Add(entry, AddEmSize);
            }
        }

        private void OnTextAnalysis(object sender, RoutedEventArgs e)
        {
            var analysis = TextAnalysis.Analyze(_boxOutput.Text, (ReadingDirection)_readingSelector.SelectedItem, _boxLocale.Text);

            new TextAnalysisWindow { DataContext = analysis, Title = "Text Analysis: " + analysis.Text }.Show();
        }

        private void OnGlyphRunDetails(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: BoxItem item })
            {
                if (item.RenderingElement == null)
                    return;

                GlyphRunDetails details = new GlyphRunDetails(item);
                RecordingRenderer renderer = new RecordingRenderer(details);
                item.RenderingElement.Render(renderer);
                new GlyphRunWindow { DataContext = renderer.Details }.Show();
            }
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
                    RenderTargetBitmap bitmap = new RenderTargetBitmap((int)el.ActualWidth, (int)el.ActualHeight, 96, 96, PixelFormats.Pbgra32);
                    bitmap.Render(el);

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
                    if (item.RenderingElement is DirectWriteElement el)
                        BindingOperations.GetBindingExpression(el, DirectWriteElement.FontSizeProperty).UpdateTarget();
                    
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
    }
}
