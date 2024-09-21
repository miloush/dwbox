using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Win32;

namespace DWBox
{
    public partial class GlyphRunWindow : Window
    {
        public GlyphRunWindow()
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(Properties.Settings.Default.LastUnits))
                Scale(Properties.Settings.Default.LastUnits);
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
            _advance.Binding = new Binding(prefix + nameof(GlyphRunDetailsItem.Advance));
            _advanceOffset.Binding = new Binding(prefix + nameof(GlyphRunDetailsItem.AdvanceOffset));
            _ascenderOffset.Binding = new Binding(prefix + nameof(GlyphRunDetailsItem.AscenderOffset));
        }
    }

    public class AlternatingClusterRowStyleSelector : StyleSelector
    {
        public Style OddStyle { get; set; }
        public Style EvenStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is GlyphRunDetailsItem detail)
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
