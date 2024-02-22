using System;
using System.Globalization;
using System.Windows.Data;

namespace DWBox
{
    public class ImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;

            Type valueType = value.GetType();
            string typeName = valueType.Name;
            string valueName = null;

            if (valueType.IsEnum)
                valueName = Enum.GetName(value.GetType(), value);

            return App.Current.FindResource(typeName + valueName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
