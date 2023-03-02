using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using Win32.DWrite;

namespace DWBox
{
    public class FeaturesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s || string.IsNullOrWhiteSpace(s))
                return null;

            string[] tags = s.Split();
            List<FontFeatureTag> parsed = new List<FontFeatureTag>(tags.Length);

            foreach (string tag in tags)
                if (TryParse(tag, out var t))
                    parsed.Add(t);

            return parsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static bool TryParse(string s, out FontFeatureTag tag)
        {
            tag = default;
            
            if (s == null) 
                return false;

            if (s.Length != 4)
                return Enum.TryParse(s, out tag);

            tag = (FontFeatureTag)DWrite.StringToTag(s);
            return true;
        }
    }
}
