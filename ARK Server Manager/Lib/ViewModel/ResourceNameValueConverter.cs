using System;
using System.Globalization;
using System.Windows.Data;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class ResourceNameValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var strVal = value as string;
            var firstIndex = strVal.IndexOf('_');
            var lastIndex = strVal.LastIndexOf('_');
            var subStr = strVal.Substring(firstIndex + 1, lastIndex - firstIndex - 1).Replace('_', ' ');
            return subStr;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
