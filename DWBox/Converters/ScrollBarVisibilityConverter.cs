using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace DWBox
{
    public class ScrollBarVisibilityConverter : IMultiValueConverter
    {
        // return priority Visible > Auto=True > Disabled
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            ScrollBarVisibility visibility = ScrollBarVisibility.Disabled;

            foreach (object value in values)
            {
                ScrollBarVisibility valueVisibility = ScrollBarVisibility.Disabled;
                if (value is true)
                    valueVisibility = ScrollBarVisibility.Auto;
                else if (value is ScrollBarVisibility explicitVisibility)
                    valueVisibility = explicitVisibility;

                if (valueVisibility == ScrollBarVisibility.Visible)
                    return valueVisibility;
                else if (valueVisibility == ScrollBarVisibility.Auto)
                    visibility = valueVisibility;
            }

            return visibility;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
