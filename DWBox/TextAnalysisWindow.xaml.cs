using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DWBox
{
    public partial class TextAnalysisWindow : Window
    {
        public TextAnalysisWindow()
        {
            InitializeComponent();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }
    }

    public class BidiRowStyleSelector : StyleSelector
    {
        public Style LeftToRightStyle { get; set; }
        public Style RightToLeftStyle { get; set; }

        public override Style SelectStyle(object item, DependencyObject container)
        {
            if (item is TextAnalysisItem detail)
            {
                if (detail.BidiResolvedLevel % 2 == 0)
                    return LeftToRightStyle;
                else
                    return RightToLeftStyle;
            }

            return base.SelectStyle(item, container);
        }
    }
}
