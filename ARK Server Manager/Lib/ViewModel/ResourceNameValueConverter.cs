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
            return strVal.Substring("PrimalItemResource_".Length, strVal.Length - "PrimalItemResource__C".Length);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
