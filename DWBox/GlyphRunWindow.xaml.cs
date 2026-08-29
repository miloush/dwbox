using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace DWBox
{
    public partial class GlyphRunWindow : Window
    {
        private BoxItem _item;

        private GlyphRunWindow()
        {
            InitializeComponent();
            Scale(Properties.Settings.Default.LastUnits);
        }

        public GlyphRunWindow(BoxItem item) : this()
        {
            _item = item;
            OnLiveUpdate();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void Scale(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menu)
            {
                Scale(menu.Tag?.ToString());

                Properties.Settings.Default.LastUnits = menu.Tag?.ToString();
                Properties.Settings.Default.Save();
            }
        }

        private void Scale(string prefix)
        {
            _advance.Binding = new Binding(prefix + nameof(RenderGlyphDetails.Advance));
            _advanceOffset.Binding = new Binding(prefix + nameof(RenderGlyphDetails.AdvanceOffset));
            _ascenderOffset.Binding = new Binding(prefix + nameof(RenderGlyphDetails.AscenderOffset));

            if (prefix == "Design")
                _designScale.IsChecked = true;
            else 
                _emScale.IsChecked = true;
        }

        private void OnLiveUpdatesChecked(object sender, RoutedEventArgs e)
        {
            if (_item?.RenderingElement is DirectWriteElement el)
            {
                el.TextLayoutChanged += OnLiveUpdate;
                OnLiveUpdate(sender);
            }
        }

        private void OnLiveUpdatesUnchecked(object sender, RoutedEventArgs e)
        {
            if (_item?.RenderingElement is DirectWriteElement el)
            {
                el.TextLayoutChanged -= OnLiveUpdate;
            }
        }

        private void OnLiveUpdate(object sender = null, EventArgs e = null)
        {
            RenderDetails details = new RenderDetails(_item.FontFace, _item.EmSize);
            DetailsRenderer renderer = new DetailsRenderer(details, App.ViewModel.IsVectorMode);
            _item.RenderingElement.Render(renderer);

            ListCollectionView view = (ListCollectionView)CollectionViewSource.GetDefaultView(renderer.Details);
            view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RenderGlyphDetails.RunDetails)));

            DataContext = view;
        }

        protected override void OnClosed(EventArgs e)
        {
            OnLiveUpdatesUnchecked(null, null);
            base.OnClosed(e);
        }

        private void OnEmScaleChecked(object sender, RoutedEventArgs e)
        {
            Scale("");
        }

        private void OnDesignScaleChecked(object sender, RoutedEventArgs e)
        {
            Scale("Design");
        }
    }

    public class AlternatingClusterRowStyleSelector : StyleSelector
    {
        public Style OddStyle { get; set; }
        public Style EvenStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is RenderGlyphDetails detail)
            {
                if (detail.ClusterIndex % 2 == 0)
                    return EvenStyle;
                else
                    return OddStyle;
            }

            return base.SelectStyle(item, container);
        }
    }
}
