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

            string valueName = value as string;
            string typeName = null;
            if (valueName == null)
            {
                Type valueType = value.GetType();
                typeName = valueType.Name;

                if (valueType.IsEnum)
                    valueName = Enum.GetName(value.GetType(), value);
            }

            return App.Current.FindResource(typeName + valueName);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
