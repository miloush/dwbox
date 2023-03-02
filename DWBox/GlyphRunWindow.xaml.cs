using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
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
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
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
