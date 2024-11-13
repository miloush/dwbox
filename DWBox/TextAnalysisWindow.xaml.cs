using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Win32.DWrite;

namespace DWBox
{
    public partial class TextAnalysisWindow : Window
    {
        MainWindow _owner;

        public TextAnalysisWindow()
        {
            InitializeComponent();
        }
        public TextAnalysisWindow(MainWindow owner) : this()
        {
            _owner = owner;
            OnLiveUpdate();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                Close();
        }

        private void OnLiveUpdatesChecked(object sender, RoutedEventArgs e)
        {
            if (_owner != null)
            {
                _owner._boxOutput.TextChanged += OnLiveUpdate;
                _owner._readingSelector.SelectionChanged += OnLiveUpdate;
                DependencyPropertyDescriptor.FromProperty(ComboBox.TextProperty, _owner._boxLocale.GetType()).AddValueChanged(_owner._boxLocale, OnLiveUpdate);
                OnLiveUpdate();
            }
        }

        private void OnLiveUpdatesUnchecked(object sender, RoutedEventArgs e)
        {
            if (_owner != null)
            {
                _owner._boxOutput.TextChanged -= OnLiveUpdate;
                _owner._readingSelector.SelectionChanged -= OnLiveUpdate;
                DependencyPropertyDescriptor.FromProperty(ComboBox.TextProperty, _owner._boxLocale.GetType()).RemoveValueChanged(_owner._boxLocale, OnLiveUpdate);
            }
        }

        private void OnLiveUpdate(object sender = null, EventArgs e = null)
        {
            var analysis = TextAnalysis.Analyze(_owner._boxOutput.Text, (ReadingDirection)_owner._readingSelector.SelectedItem, _owner._boxLocale.Text);
            Title = "Text Analysis: " + analysis.Text;
            DataContext = analysis;
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
