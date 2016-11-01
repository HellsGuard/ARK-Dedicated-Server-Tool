using System;
using System.Windows.Data;
using System.Globalization;
using System.Linq;

namespace ARK_Server_Manager.Lib.ViewModel
{
    public class AllTrueMultiValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            return !values.Any(v => !(v is bool) || !(bool)v);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("AllTrueConverter is a OneWay converter.");
        }
    }
}